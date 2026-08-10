using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Internal;
using RazorLight.Text;

namespace RazorLight
{
	/// <summary>Defines the mutable page contract consumed by generated and precompiled templates.</summary>
	public interface ITemplatePage
	{
		/// <summary>Assigns the model used by the generated page.</summary>
		void SetModel(object? model);

		/// <summary>
		/// Gets or sets the view context of the rendering template.
		/// </summary>
		PageContext? PageContext { get; set; }

		/// <summary>Gets the cancellation token for the current render operation.</summary>
		CancellationToken CancellationToken => PageContext?.CancellationToken ?? System.Threading.CancellationToken.None;

		/// <summary>
		/// Gets or sets the body content.
		/// </summary>
		ITemplateContent? BodyContent { get; set; }

		/// <summary>
		/// Gets or sets the output encoder used for expression values.
		/// </summary>
		IOutputEncoder OutputEncoder { get; set; }

		/// <summary>
		/// Gets or sets the unique key of the current template
		/// </summary>
		string? Key { get; set; }

		/// <summary>
		/// Gets or sets a flag that determines if the layout of this page is being rendered.
		/// </summary>
		/// <remarks>
		/// Sections defined in a page are deferred and executed as part of the layout page.
		/// When this flag is set, all write operations performed by the page are part of a
		/// section being rendered.
		/// </remarks>
		bool IsLayoutBeingRendered { get; set; }

		/// <summary>
		/// Gets or sets the key of a layout page.
		/// </summary>
		string? Layout { get; set; }

		/// <summary>
		/// Gets or sets the sections that can be rendered by this page.
		/// </summary>
		IDictionary<string, RenderAsyncDelegate>? PreviousSectionWriters { get; set; }

		/// <summary>
		/// Gets the sections that are defined by this page.
		/// </summary>
		IDictionary<string, RenderAsyncDelegate> SectionWriters { get; }

		/// <summary>
		/// Renders the page and writes the output to the <see cref="IPageContext.Writer"/>.
		/// </summary>
		/// <returns>A task representing the result of executing the page.</returns>
		Task ExecuteAsync();

		/// <summary>Gets or sets the callback used to render an included template in the current scope.</summary>
		Func<string, object?, CancellationToken, Task>? IncludeFunc { get; set; }

		/// <summary>Ensures the body and required sections were consumed by the layout.</summary>
		void EnsureRenderedBodyOrSections();
	}
}
