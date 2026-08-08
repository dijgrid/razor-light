using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace RazorLight.Compilation
{
	internal sealed class PrecompiledTemplateCompiler : IRazorTemplateCompiler
	{
		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey) =>
			Missing(templateKey, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, CancellationToken cancellationToken) =>
			Missing(templateKey, cancellationToken);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType) =>
			Missing(templateKey, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType, CancellationToken cancellationToken) =>
			Missing(templateKey, cancellationToken);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, string templateContent, Type? modelType = null) =>
			UnsupportedStringTemplate(templateKey, CancellationToken.None);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		public Task<CompiledTemplateDescriptor> CompileAsync(
			string templateKey,
			string templateContent,
			Type? modelType,
			CancellationToken cancellationToken) =>
			UnsupportedStringTemplate(templateKey, cancellationToken);

		private static Task<CompiledTemplateDescriptor> Missing(string key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			throw new TemplateNotFoundException(
				$"No precompiled template was found for key '{key}'. Precompiled-only mode never falls back to runtime compilation.");
		}

		private static Task<CompiledTemplateDescriptor> UnsupportedStringTemplate(string key, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			throw new RazorLightException(
				$"Template '{key}' supplied runtime source content, which is not supported in precompiled-only mode.");
		}
	}
}
