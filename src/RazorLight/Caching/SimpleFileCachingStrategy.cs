using System.IO;
using RazorLight.Razor;

namespace RazorLight.Caching
{
	public sealed class SimpleFileCachingStrategy : IFileSystemCachingStrategy
	{
		public static readonly IFileSystemCachingStrategy Instance = new SimpleFileCachingStrategy();

		public string Name => "Simple";

		public CachedFileInfo GetCachedFileInfo(string key, string templateFilePath, string cacheDir)
		{
			var asmFilePath = ResolveCachePath(key + ".dll", "cached assembly path");
			var pdbFilePath = ResolveCachePath(key + ".pdb", "cached symbol path");
			var upToDate = false;
			if (File.Exists(asmFilePath))
			{
				var templateFileTime = File.GetLastWriteTimeUtc(templateFilePath);
				var asmFileTime = File.GetLastWriteTimeUtc(asmFilePath);
				upToDate = templateFileTime < asmFileTime;
			}
			return new CachedFileInfo(upToDate, asmFilePath, pdbFilePath);

			string ResolveCachePath(string cacheKey, string parameterDescription)
			{
				var fullPath = FileSystemRazorProjectHelper.ResolveContainedPath(
					cacheDir,
					cacheKey,
					parameterDescription);

				return Path.IsPathFullyQualified(cacheDir)
					? fullPath
					: Path.GetRelativePath(System.Environment.CurrentDirectory, fullPath);
			}
		}
	}
}
