namespace RazorLight.Caching
{
	/// <summary>Describes the assembly and symbol paths selected by a disk-cache strategy.</summary>
	public struct CachedFileInfo
	{
		/// <summary>Indicates whether the selected artifact matches current inputs.</summary>
		public readonly bool UpToDate;
		/// <summary>Gets the compiled assembly path.</summary>
		public readonly string AssemblyFilePath;
		/// <summary>Gets the optional portable-symbol path.</summary>
		public readonly string PdbFilePath;

		/// <summary>Creates a disk-cache lookup result.</summary>
		public CachedFileInfo(bool upToDate, string assemblyFilePath, string pdbFilePath)
		{
			UpToDate = upToDate;
			AssemblyFilePath = assemblyFilePath;
			PdbFilePath = pdbFilePath;
		}

		/// <summary>Deconstructs the lookup result into freshness and artifact paths.</summary>
		public void Deconstruct(out bool upToDate, out string assemblyFilePath, out string pdbFilePath)
		{
			upToDate = UpToDate;
			assemblyFilePath = AssemblyFilePath;
			pdbFilePath = PdbFilePath;
		}
	}

	/// <summary>Selects deterministic compiled artifact paths for file-system templates.</summary>
	public interface IFileSystemCachingStrategy
	{
		/// <summary>Gets the stable strategy name recorded in cache metadata.</summary>
		string Name { get; }
		/// <summary>Determines current artifact paths and whether their inputs are up to date.</summary>
		CachedFileInfo GetCachedFileInfo(string key, string templateFilePath, string cacheDir);
	}
}
