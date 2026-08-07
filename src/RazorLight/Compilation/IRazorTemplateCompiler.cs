using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace RazorLight.Compilation
{
	public interface IRazorTemplateCompiler
	{
		ICompilationService CompilationService { get; }

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType);

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, string templateContent, Type? modelType = null);
	}
}
