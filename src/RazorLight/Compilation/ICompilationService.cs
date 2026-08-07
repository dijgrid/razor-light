using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using RazorLight.Generation;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RazorLight.Compilation
{
	public interface ICompilationService
	{
		CSharpCompilationOptions CSharpCompilationOptions { get; }
		EmitOptions EmitOptions { get; }
		CSharpParseOptions ParseOptions { get; }
		Assembly OperatingAssembly { get; }

		[RequiresDynamicCode(DeploymentCompatibility.RequiresDynamicCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		[RequiresUnreferencedCode(DeploymentCompatibility.RequiresUnreferencedCodeMessage, Url = DeploymentCompatibility.DocumentationUrl)]
		Assembly CompileAndEmit(IGeneratedRazorTemplate razorTemplate);
	}
}
