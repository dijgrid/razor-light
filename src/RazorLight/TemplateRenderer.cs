using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Internal.Buffering;

namespace RazorLight
{
	internal sealed class TemplateRenderer
	{
		private readonly IEngineHandler _engineHandler;
		private readonly IViewBufferScope _bufferScope;
		private readonly Action<ITemplatePage>? _pageInitializer;
		private readonly HashSet<ITemplatePage> _initializedPages = new HashSet<ITemplatePage>(ReferenceEqualityComparer.Instance);

		public TemplateRenderer(
			IEngineHandler engineHandler,
			IViewBufferScope bufferScope) : this(engineHandler, bufferScope, pageInitializer: null)
		{
		}

		internal TemplateRenderer(
			IEngineHandler engineHandler,
			IViewBufferScope bufferScope,
			Action<ITemplatePage>? pageInitializer)
		{
			_engineHandler = engineHandler ?? throw new ArgumentNullException(nameof(engineHandler));
			_bufferScope = bufferScope ?? throw new ArgumentNullException(nameof(bufferScope));
			_pageInitializer = pageInitializer;
		}

		public Task RenderAsync(ITemplatePage page) =>
			RenderAsync(page, page.PageContext?.CancellationToken ?? CancellationToken.None);

		public async Task RenderAsync(ITemplatePage page, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var context = page.PageContext ?? throw new InvalidOperationException("The template page has no PageContext.");
			context.CancellationToken = cancellationToken;

			var bodyWriter = await RenderPageAsync(page, context, cancellationToken).ConfigureAwait(false);
			await RenderLayoutAsync(page, context, bodyWriter, cancellationToken).ConfigureAwait(false);
		}

		private async Task<ViewBufferTextWriter> RenderPageAsync(
			ITemplatePage page,
			PageContext context,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var writer = context.Writer as ViewBufferTextWriter;
			if (writer == null)
			{
				Debug.Assert(_bufferScope != null);

				// If we get here, this is likely the top-level page (not a partial) - this means
				// that context.Writer is wrapping the output stream. We need to buffer, so create a buffered writer.
				var buffer = new ViewBuffer(_bufferScope, page.Key, ViewBuffer.ViewPageSize);
				writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding, context.Writer);
			}
			else
			{
				// This means we're writing something like a partial, where the output needs to be buffered.
				// Create a new buffer, but without the ability to flush.
				var buffer = new ViewBuffer(_bufferScope, page.Key, ViewBuffer.ViewPageSize);
				writer = new ViewBufferTextWriter(buffer, context.Writer.Encoding);
			}

			// The writer for the body is passed through the PageContext, allowing things like HtmlHelpers
			// and ViewComponents to reference it.
			var oldWriter = context.Writer;
			var oldFilePath = context.ExecutingPageKey;

			context.Writer = writer;
			context.ExecutingPageKey = page.Key;

			try
			{
				if (_initializedPages.Add(page))
				{
					_pageInitializer?.Invoke(page);
				}

				await RenderPageCoreAsync(page, context, cancellationToken).ConfigureAwait(false);
				return writer;
			}
			finally
			{
				context.Writer = oldWriter;
				context.ExecutingPageKey = oldFilePath;
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Precompiled-only handlers resolve includes from registered page factories; runtime handlers expose the trim warning at their public entry points.")]
		private async Task RenderPageCoreAsync(ITemplatePage page, PageContext context, CancellationToken cancellationToken)
		{
			page.PageContext = context;
			page.IncludeFunc = async (key, model, includeCancellationToken) =>
			{
				ITemplatePage template = await _engineHandler
					.CompileTemplateAsync(key, includeCancellationToken)
					.ConfigureAwait(false);

				await _engineHandler.RenderIncludedTemplateAsync(
					template,
					model,
					context.Writer,
					context.ViewBagData,
					this,
					includeCancellationToken).ConfigureAwait(false);
			};

			await page.ExecuteAsync().ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
		}

		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Precompiled-only handlers resolve layouts from registered page factories; runtime handlers expose the trim warning at their public entry points.")]
		private async Task RenderLayoutAsync(
			ITemplatePage page,
			PageContext context,
			ViewBufferTextWriter bodyWriter,
			CancellationToken cancellationToken)
		{
			// A layout page can specify another layout page. We'll need to continue
			// looking for layout pages until they're no longer specified.
			var previousPage = page;
			var renderedLayouts = new List<ITemplatePage>();

			// This loop will execute Layout pages from the inside to the outside. With each
			// iteration, bodyWriter is replaced with the aggregate of all the "body" content
			// (including the layout page we just rendered).
			while (!string.IsNullOrEmpty(previousPage.Layout))
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!bodyWriter.IsBuffering)
				{
					// Once a call to RazorPage.FlushAsync is made, we can no longer render Layout pages - content has
					// already been written to the client and the layout content would be appended rather than surround
					// the body content. Throwing this exception wouldn't return a 500 (since content has already been
					// written), but a diagnostic component should be able to capture it.

					throw new InvalidOperationException("Layout can not be rendered");
				}

				ITemplatePage layoutPage = await _engineHandler.CompileTemplateAsync(previousPage.Layout, cancellationToken).ConfigureAwait(false);
				layoutPage.SetModel(context.Model);

				if (renderedLayouts.Count > 0 &&
					renderedLayouts.Any(l => string.Equals(l.Key, layoutPage.Key, StringComparison.Ordinal)))
				{
					// If the layout has been previously rendered as part of this view, we're potentially in a layout
					// rendering cycle.
					throw new InvalidOperationException($"Layout {layoutPage.Key} has circular reference");
				}

				// Notify the previous page that any writes that are performed on it are part of sections being written
				// in the layout.
				previousPage.IsLayoutBeingRendered = true;
				layoutPage.PreviousSectionWriters = previousPage.SectionWriters;
				layoutPage.BodyContent = bodyWriter.Buffer;
				bodyWriter = await RenderPageAsync(layoutPage, context, cancellationToken).ConfigureAwait(false);

				renderedLayouts.Add(layoutPage);
				previousPage = layoutPage;
			}

			// Now we've reached and rendered the outer-most layout page. Nothing left to execute.
			// Ensure all defined sections were rendered or RenderBody was invoked for page without defined sections.
			foreach (var layoutPage in renderedLayouts)
			{
				layoutPage.EnsureRenderedBodyOrSections();
			}

			if (bodyWriter.IsBuffering)
			{
				cancellationToken.ThrowIfCancellationRequested();
				// If IsBuffering - then we've got a bunch of content in the view buffer. How to best deal with it
				// really depends on whether or not we're writing directly to the output or if we're writing to
				// another buffer.
				var viewBufferTextWriter = context.Writer as ViewBufferTextWriter;
				if (viewBufferTextWriter == null || !viewBufferTextWriter.IsBuffering)
				{
					// This means we're writing to a 'real' writer, probably to the actual output stream.
					// Smooth synchronous writes of final template-content values.
					using (var writer = _bufferScope.CreateWriter(context.Writer))
					{
						await bodyWriter.Buffer.WriteToAsync(writer, cancellationToken).ConfigureAwait(false);
					}
				}
				else
				{
					// This means we're writing to another buffer. Use MoveTo to combine them.
					bodyWriter.Buffer.MoveTo(viewBufferTextWriter.Buffer);
				}
			}
		}

	}
}
