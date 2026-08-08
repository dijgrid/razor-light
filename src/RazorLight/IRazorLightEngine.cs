using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight
{
	public interface IRazorLightEngine : IDisposable, IAsyncDisposable
	{
		void IDisposable.Dispose() { }

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			((IDisposable)this).Dispose();
			return ValueTask.CompletedTask;
		}
		/// <summary>
		/// Returns whether the engine has a cached template for <paramref name="key"/>.
		/// </summary>
		bool IsTemplateCached(string key);

		/// <summary>
		/// Invalidates all compiled variants and page factories associated with
		/// <paramref name="key"/>. Invalidating an unknown key is safe.
		/// </summary>
		void InvalidateTemplate(string key);

		/// <summary>
		/// Compiles and renders a template with a given <paramref name="key"/>
		/// </summary>
		/// <typeparam name="T">Type of the model</typeparam>
		/// <param name="key">Unique key of the template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the template</param>
		/// <returns>Rendered template as a string result</returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag = null);

		/// <summary>Compiles and renders a template while observing caller cancellation.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync<T>(string key, T model, CancellationToken cancellationToken) =>
			CompileRenderAsync(key, model, viewBag: null, cancellationToken);

		/// <summary>Compiles and renders a template while observing caller cancellation.</summary>
		/// <remarks>Cancellation stops this caller's wait for shared compilation; it does not cancel work still used by another caller.</remarks>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await CompileRenderAsync(key, model, viewBag).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles and renders a project template using <paramref name="modelType"/> when the template
		/// does not declare an explicit <c>@model</c> directive.
		/// </summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag = null);

		/// <summary>Compiles and renders a project template while observing caller cancellation.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync(string key, object? model, Type modelType, CancellationToken cancellationToken) =>
			CompileRenderAsync(key, model, modelType, viewBag: null, cancellationToken);

		/// <summary>Compiles and renders a project template while observing caller cancellation.</summary>
		/// <remarks>Cancellation stops this caller's wait for shared compilation; it does not cancel work still used by another caller.</remarks>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await CompileRenderAsync(key, model, modelType, viewBag).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles and renders a template. Template content is taken directly from <paramref name="content"/> parameter
		/// </summary>
		/// <typeparam name="T">Type of the model</typeparam>
		/// <param name="key">Unique key of the template</param>
		/// <param name="content">Content of the template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic ViewBag</param>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag = null);

		/// <summary>Compiles and renders string content while observing caller cancellation.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync<T>(string key, string content, T model, CancellationToken cancellationToken) =>
			CompileRenderStringAsync(key, content, model, viewBag: null, cancellationToken);

		/// <summary>Compiles and renders string content while observing caller cancellation.</summary>
		/// <remarks>Cancellation stops this caller's wait for shared compilation; it does not cancel work still used by another caller.</remarks>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await CompileRenderStringAsync(key, content, model, viewBag).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles and renders string content using <paramref name="modelType"/> when the template does
		/// not declare an explicit <c>@model</c> directive.
		/// </summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null);

		/// <summary>Compiles and renders string content while observing caller cancellation.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			CancellationToken cancellationToken) =>
			CompileRenderStringAsync(key, content, model, modelType, viewBag: null, cancellationToken);

		/// <summary>Compiles and renders string content while observing caller cancellation.</summary>
		/// <remarks>Cancellation stops this caller's wait for shared compilation; it does not cancel work still used by another caller.</remarks>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await CompileRenderStringAsync(key, content, model, modelType, viewBag)
				.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Search and compile a template with a given key
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <returns>An instance of a template</returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<ITemplatePage> CompileTemplateAsync(string key);

		/// <summary>Searches for and compiles a template while observing caller cancellation.</summary>
		/// <remarks>Cancellation stops this caller's wait for shared compilation; it does not cancel work still used by another caller.</remarks>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<ITemplatePage> CompileTemplateAsync(string key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await CompileTemplateAsync(key).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Compiles a template and returns a reusable handle that creates a fresh page for every render.
		/// </summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		async Task<RazorLightTemplate> CompileReusableTemplateAsync(
			string key,
			CancellationToken cancellationToken = default)
		{
			await CompileTemplateAsync(key, cancellationToken).ConfigureAwait(false);
			return new RazorLightTemplate(this, key);
		}

		/// <summary>
		/// Renders a template with a given model
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the template</param>
		/// <returns>Rendered string</returns>
		Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null);

		/// <summary>Renders a template while observing caller cancellation.</summary>
		Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, CancellationToken cancellationToken) =>
			RenderTemplateAsync(templatePage, model, viewBag: null, cancellationToken);

		/// <summary>Renders a template and checks cancellation at safe page, include, layout, and output boundaries.</summary>
		async Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await RenderTemplateAsync(templatePage, model, viewBag).WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Renders a template to the specified <paramref name="textWriter"/>
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the page</param>
		/// <param name="textWriter">Output</param>
		Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag = null);

		/// <summary>Renders a template to a writer while observing caller cancellation.</summary>
		Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			CancellationToken cancellationToken) =>
			RenderTemplateAsync(templatePage, model, textWriter, viewBag: null, cancellationToken);

		/// <summary>Renders a template to a writer and observes cancellation where the operation can stop safely.</summary>
		async Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await RenderTemplateAsync(templatePage, model, textWriter, viewBag)
				.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
	}
}
