using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace RazorLight
{
	public interface IRazorLightEngine
	{
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

		/// <summary>
		/// Compiles and renders a project template using <paramref name="modelType"/> when the template
		/// does not declare an explicit <c>@model</c> directive.
		/// </summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag = null);

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

		/// <summary>
		/// Search and compile a template with a given key
		/// </summary>
		/// <param name="key">Unique key of the template</param>
		/// <returns>An instance of a template</returns>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<ITemplatePage> CompileTemplateAsync(string key);

		/// <summary>
		/// Renders a template with a given model
		/// </summary>
		/// <param name="templatePage">Instance of a template</param>
		/// <param name="model">Template model</param>
		/// <param name="viewBag">Dynamic viewBag of the template</param>
		/// <returns>Rendered string</returns>
		Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null);

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
	}
}
