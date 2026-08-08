using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Caching;
using RazorLight.Compilation;

namespace RazorLight
{
	internal interface IEngineHandler
	{
		ICachingProvider? Cache { get; }
		IRazorTemplateCompiler Compiler { get; }
		ITemplateFactoryProvider FactoryProvider { get; }

		RazorLightOptions Options { get; }

		[MemberNotNullWhen(true, nameof(Cache))]
		bool IsCachingEnabled { get; }

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<ITemplatePage> CompileTemplateAsync(string key);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<ITemplatePage> CompileTemplateAsync(string key, CancellationToken cancellationToken);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag = null);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag, CancellationToken cancellationToken);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag = null);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag, CancellationToken cancellationToken);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag = null);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag, CancellationToken cancellationToken);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync(string key, string content, object? model, Type modelType, ExpandoObject? viewBag = null);
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<string> CompileRenderStringAsync(string key, string content, object? model, Type modelType, ExpandoObject? viewBag, CancellationToken cancellationToken);

		Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null);
		Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag, CancellationToken cancellationToken);
		Task RenderTemplateAsync<T>(ITemplatePage templatePage, T model, TextWriter textWriter, ExpandoObject? viewBag = null);
		Task RenderTemplateAsync<T>(ITemplatePage templatePage, T model, TextWriter textWriter, ExpandoObject? viewBag, CancellationToken cancellationToken);
		Task RenderIncludedTemplateAsync<T>(ITemplatePage templatePage, T model, TextWriter textWriter, ExpandoObject? viewBag, TemplateRenderer templateRenderer, CancellationToken cancellationToken);
	}
}
