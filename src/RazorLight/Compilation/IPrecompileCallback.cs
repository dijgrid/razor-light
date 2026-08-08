using RazorLight.Generation;

namespace RazorLight.Compilation
{
	internal interface IPrecompileCallback
	{
		void Invoke(IGeneratedRazorTemplate generatedRazorTemplate, byte[] rawAssembly, byte[] rawSymbolStore);
	}
}
