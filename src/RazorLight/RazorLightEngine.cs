using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.DependencyInjection;

namespace RazorLight
{
	internal sealed class RazorLightEngine : IRazorLightEngine
	{
		private readonly IEngineHandler _handler;
		private readonly TemplateCache _cache;

		public RazorLightEngine(IEngineHandler handler)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
			_cache = new TemplateCache(_handler.Cache);
		}

		internal IEngineHandler Handler => _handler;
		internal RazorLightOptions Options => _handler.Options;

		public bool IsTemplateCached(string key) => _cache.Contains(key);

		public void InvalidateTemplate(string key) => _cache.Remove(key);

		public void Dispose() => (_handler as IDisposable)?.Dispose();

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}

		internal void ConfigureServices(IServiceScopeFactory scopeFactory, PropertyInjector propertyInjector)
		{
			if (_handler is not EngineHandler engineHandler)
			{
				throw new InvalidOperationException("Dependency injection requires the built-in RazorLight engine handler.");
			}

			engineHandler.ConfigureServices(scopeFactory, propertyInjector);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag = null)
		{
			return CompileRenderAsync(key, model, modelType, viewBag, CancellationToken.None);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.CompileRenderAsync(key, model, modelType, viewBag, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag = null)
		{
			return CompileRenderAsync(key, model, viewBag, CancellationToken.None);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.CompileRenderAsync(key, model, viewBag, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync<T>(
			string key,
			string content,
			T model,
			ExpandoObject? viewBag = null)
		{
			return CompileRenderStringAsync(key, content, model, viewBag, CancellationToken.None);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.CompileRenderStringAsync(key, content, model, viewBag, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync(
			string key,
			string content,
			object? model,
			Type modelType,
			ExpandoObject? viewBag = null)
		{
			return CompileRenderStringAsync(key, content, model, modelType, viewBag, CancellationToken.None);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<string> CompileRenderStringAsync(string key, string content, object? model, Type modelType, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.CompileRenderStringAsync(key, content, model, modelType, viewBag, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<ITemplatePage> CompileTemplateAsync(string key)
		{
			return CompileTemplateAsync(key, CancellationToken.None);
		}

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<ITemplatePage> CompileTemplateAsync(string key, CancellationToken cancellationToken) =>
			_handler.CompileTemplateAsync(key, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		public Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null)
		{
			return RenderTemplateAsync(templatePage, model, viewBag, CancellationToken.None);
		}

		public Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.RenderTemplateAsync(templatePage, model, viewBag, cancellationToken);

		/// <inheritdoc cref="IRazorLightEngine"/>
		public Task RenderTemplateAsync<T>(
			ITemplatePage templatePage,
			T model,
			TextWriter textWriter,
			ExpandoObject? viewBag = null)
		{
			return RenderTemplateAsync(templatePage, model, textWriter, viewBag, CancellationToken.None);
		}

		public Task RenderTemplateAsync<T>(ITemplatePage templatePage, T model, TextWriter textWriter, ExpandoObject? viewBag, CancellationToken cancellationToken) =>
			_handler.RenderTemplateAsync(templatePage, model, textWriter, viewBag, cancellationToken);
	}
}
