using System;
using System.Threading.Tasks;

namespace RazorLight.Compilation
{
	public interface IRazorTemplateCompiler
	{
		ICompilationService CompilationService { get; }

		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey);

		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, Type modelType);

		Task<CompiledTemplateDescriptor> CompileAsync(string templateKey, string templateContent, Type? modelType = null);
	}
}
