using NUnit.Framework;
using System.Diagnostics;

namespace RazorLight.Precompile.Tests
{
	public class PrecompileTests : TestWithCulture
	{
		[TestCaseSource(typeof(PrecompileTestCases), nameof(PrecompileTestCases.TestCases))]
		public void PrecompileFromScratch(string templateFilePath, TestScenario scenario)
		{
			scenario.Cleanup();

			var expectedPrecompiledFilePath = GetExpectedPrecompiledFilePath(templateFilePath, scenario);
			Assert.That(File.Exists(expectedPrecompiledFilePath), Is.False);

			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);

			scenario.Cleanup();
			Assert.That(File.Exists(expectedPrecompiledFilePath), Is.False);
		}

		[TestCaseSource(typeof(PrecompileTestCases), nameof(PrecompileTestCases.TestCases))]
		public void PrecompileCached(string templateFilePath, TestScenario scenario)
		{
			scenario.Cleanup();

			var expectedPrecompiledFilePath = GetExpectedPrecompiledFilePath(templateFilePath, scenario);
			Assert.That(File.Exists(expectedPrecompiledFilePath), Is.False);

			var sw1 = Stopwatch.StartNew();
			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);
			sw1.Stop();

			Assert.That(File.Exists(expectedPrecompiledFilePath), Is.True);

			var sw2 = Stopwatch.StartNew();
			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);
			sw2.Stop();

			TestContext.WriteLine($"TS1 = {sw1.Elapsed}, TS2 = {sw2.Elapsed}");

			scenario.Cleanup();
			Assert.That(File.Exists(expectedPrecompiledFilePath), Is.False);
		}

		public static string GetExpectedPrecompiledFilePath(string templateFilePath, TestScenario scenario)
		{
			var cacheDirectory = scenario.GetExpectedCacheDirectory(templateFilePath)
				?? throw new InvalidOperationException("The scenario has no expected cache directory.");
			var cacheFileInfo = scenario.ExpectedCachingStrategy.GetCachedFileInfo(scenario.GetTemplateKey(templateFilePath), templateFilePath, cacheDirectory);
			return scenario.GetExpectedPrecompiledFilePath(cacheFileInfo.AssemblyFilePath);
		}

		public static void Precompile(string templateFilePath, TestScenario scenario, string? expectedPrecompiledFilePath)
		{
			var commandLineArgs = new List<string>
			{
				"precompile",
				"-t",
				templateFilePath
			};
			commandLineArgs.AddRange(scenario.ExtraCommandLineArgs);

			var precompiledFilePath = Helper.RunCommandTrimNewline(commandLineArgs.ToArray());
			Assert.AreEqual(expectedPrecompiledFilePath, precompiledFilePath);
			Assert.That(File.Exists(precompiledFilePath), Is.True);
		}
	}
}
