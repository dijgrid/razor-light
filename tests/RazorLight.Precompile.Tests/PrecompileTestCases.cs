using RazorLight.Caching;

namespace RazorLight.Precompile.Tests
{
	public static class PrecompileTestCases
	{
		public static void CleanupDlls(string dir)
		{
			foreach (var filePath in Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories))
			{
				File.Delete(filePath);
			}
		}

		public const string CACHE_DIR = "Cache";

		public static readonly TestScenario AllDefaults = new(
			"{m}({0})",
			FileHashCachingStrategy.Instance,
			Path.GetDirectoryName,
			Path.GetFullPath,
			Path.GetFileName,
			Array.Empty<string>(),
			() => CleanupDlls("Samples"));

		public static readonly TestScenario WithCache = new(
			"{m}_cache({0})",
			FileHashCachingStrategy.Instance,
			_ => CACHE_DIR,
			s => s,
			AllDefaults.GetTemplateKey,
			new[] { "-c", CACHE_DIR },
			() =>
			{
				if (Directory.Exists(CACHE_DIR))
				{
					Directory.Delete(CACHE_DIR, true);
				}
			});

		public static readonly TestScenario WithBase = new(
			"{m}_base({0})",
			FileHashCachingStrategy.Instance,
			AllDefaults.GetExpectedCacheDirectory,
			AllDefaults.GetExpectedPrecompiledFilePath,
			s => s,
			new[] { "-b", "." },
			() => CleanupDlls("SamplesWithBaseDir"));

		public static readonly TestScenario WithCacheAndBase = new(
			"{m}_cache_base({0})",
			FileHashCachingStrategy.Instance,
			WithCache.GetExpectedCacheDirectory,
			WithCache.GetExpectedPrecompiledFilePath,
			WithBase.GetTemplateKey,
			new[] { "-b", ".", "-c", CACHE_DIR },
			WithCache.Cleanup);

		public static readonly TestScenario WithStrategyFileHash = new(
			"{m}_strategy({0}, FileHash)",
			FileHashCachingStrategy.Instance,
			AllDefaults.GetExpectedCacheDirectory,
			AllDefaults.GetExpectedPrecompiledFilePath,
			AllDefaults.GetTemplateKey,
			new[] { "-s", "FileHash" },
			AllDefaults.Cleanup);

		public static readonly TestScenario WithStrategySimple = new(
			"{m}_strategy({0}, Simple)",
			SimpleFileCachingStrategy.Instance,
			AllDefaults.GetExpectedCacheDirectory,
			AllDefaults.GetExpectedPrecompiledFilePath,
			AllDefaults.GetTemplateKey,
			new[] { "-s", "Simple" },
			AllDefaults.Cleanup);

		public static readonly TestScenario WithCacheAndStrategyFileHash = new(
			"{m}_cache_strategy({0}, FileHash)",
			FileHashCachingStrategy.Instance,
			WithCache.GetExpectedCacheDirectory,
			WithCache.GetExpectedPrecompiledFilePath,
			WithCache.GetTemplateKey,
			new[] { "-s", "FileHash", "-c", CACHE_DIR },
			WithCache.Cleanup);

		public static readonly TestScenario WithCacheAndStrategySimple = new(
			"{m}_cache_strategy({0}, Simple)",
			SimpleFileCachingStrategy.Instance,
			WithCache.GetExpectedCacheDirectory,
			WithCache.GetExpectedPrecompiledFilePath,
			WithCache.GetTemplateKey,
			new[] { "-s", "Simple", "-c", CACHE_DIR },
			WithCache.Cleanup);

		public static readonly TestScenario WithCacheAndBaseAndStrategySimple = new(
			"{m}_cache_base_strategy({0}, Simple)",
			SimpleFileCachingStrategy.Instance,
			WithCache.GetExpectedCacheDirectory,
			WithCache.GetExpectedPrecompiledFilePath,
			WithBase.GetTemplateKey,
			new[] { "-s", "Simple", "-c", CACHE_DIR, "-b", "." },
			WithCache.Cleanup);

		public static IEnumerable<object[]> TestCases => new object[][]
		{
			new object[] { "Samples/WorkItemFields.json", AllDefaults },
			new object[] { "Samples/FullMessage.cshtml", AllDefaults },
			new object[] { "Samples/folder/MessageItem.cshtml", AllDefaults },
			new object[] { "Samples/WorkItemComment.cshtml", AllDefaults },
			new object[] { "Samples/CSharpComposition.cshtml", AllDefaults },
			new object[] { "Samples/FullMessage.cshtml", WithCache },
			new object[] { "Samples/folder/MessageItem.cshtml", WithCache },
			new object[] { "Samples/FullMessage.cshtml", WithStrategyFileHash },
			new object[] { "Samples/folder/MessageItem.cshtml", WithStrategyFileHash },
			new object[] { "Samples/FullMessage.cshtml", WithStrategySimple },
			new object[] { "Samples/folder/MessageItem.cshtml", WithStrategySimple },
			new object[] { "Samples/FullMessage.cshtml", WithCacheAndStrategyFileHash },
			new object[] { "Samples/folder/MessageItem.cshtml", WithCacheAndStrategySimple },
			new object[] { "SamplesWithBaseDir/FullMessage.cshtml", WithBase },
			new object[] { "SamplesWithBaseDir/MessageItem.cshtml", WithBase },
			new object[] { "SamplesWithBaseDir/FullMessage.cshtml", WithCacheAndBase },
			new object[] { "SamplesWithBaseDir/MessageItem.cshtml", WithCacheAndBase },
			new object[] { "SamplesWithBaseDir/FullMessage.cshtml", WithCacheAndBaseAndStrategySimple },
		};
	}
}
