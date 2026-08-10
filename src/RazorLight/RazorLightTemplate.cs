using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight
{
	/// <summary>
	/// A reusable compiled-template handle. Each render creates a fresh mutable page instance.
	/// </summary>
	public sealed class RazorLightTemplate
	{
		private readonly IRazorLightEngine _engine;

		internal RazorLightTemplate(IRazorLightEngine engine, string key)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Key = key ?? throw new ArgumentNullException(nameof(key));
		}

		/// <summary>Gets the logical key of the cached compiled template.</summary>
		public string Key { get; }

		/// <summary>Creates a fresh page instance from the cached compiled descriptor.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<ITemplatePage> CreatePageAsync(CancellationToken cancellationToken = default) =>
			_engine.CompileTemplateAsync(Key, cancellationToken);

		/// <summary>Renders the cached template using a new page that is safe for one render.</summary>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public async Task<string> RenderAsync<T>(
			T model,
			ExpandoObject? viewBag = null,
			CancellationToken cancellationToken = default)
		{
			ITemplatePage page = await CreatePageAsync(cancellationToken).ConfigureAwait(false);
			return await _engine.RenderTemplateAsync(page, model, viewBag, cancellationToken).ConfigureAwait(false);
		}
	}
}
