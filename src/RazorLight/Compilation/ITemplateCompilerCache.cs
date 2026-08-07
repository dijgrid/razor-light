namespace RazorLight.Compilation
{
	internal interface ITemplateCompilerCache
	{
		string NormalizeKey(string key);

		void Remove(string key);
	}

	internal sealed class RazorTemplateCompilerCache : ITemplateCompilerCache
	{
		private readonly RazorTemplateCompiler _compiler;

		public RazorTemplateCompilerCache(RazorTemplateCompiler compiler)
		{
			_compiler = compiler;
		}

		public string NormalizeKey(string key) => _compiler.NormalizeCacheKey(key);

		public void Remove(string key) => _compiler.RemoveCacheKey(key);
	}
}
