using System.Diagnostics;
using Xunit;

namespace RazorLight.Precompile.Tests
{
	public class PrecompileTests : TestWithCulture
	{
		[Theory]
		[MemberData(nameof(PrecompileTestCases.TestCases), MemberType = typeof(PrecompileTestCases))]
		public void PrecompileFromScratch(string templateFilePath, TestScenario scenario)
		{
			scenario.Cleanup();

			var expectedPrecompiledFilePath = GetExpectedPrecompiledFilePath(templateFilePath, scenario);
			Assert.False(File.Exists(expectedPrecompiledFilePath));

			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);

			scenario.Cleanup();
			Assert.False(File.Exists(expectedPrecompiledFilePath));
		}

		[Theory]
		[MemberData(nameof(PrecompileTestCases.TestCases), MemberType = typeof(PrecompileTestCases))]
		public void PrecompileCached(string templateFilePath, TestScenario scenario)
		{
			scenario.Cleanup();

			var expectedPrecompiledFilePath = GetExpectedPrecompiledFilePath(templateFilePath, scenario);
			Assert.False(File.Exists(expectedPrecompiledFilePath));

			var sw1 = Stopwatch.StartNew();
			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);
			sw1.Stop();

			Assert.True(File.Exists(expectedPrecompiledFilePath));

			var sw2 = Stopwatch.StartNew();
			Precompile(templateFilePath, scenario, expectedPrecompiledFilePath);
			sw2.Stop();

			Debug.WriteLine($"TS1 = {sw1.Elapsed}, TS2 = {sw2.Elapsed}");

			scenario.Cleanup();
			Assert.False(File.Exists(expectedPrecompiledFilePath));
		}

		[Fact]
		public void Precompile_Rejects_Template_Outside_Explicit_Base_Directory()
		{
			var command = new PrecompileCmd();

			Assert.Throws<InvalidOperationException>(() => command.Run(new[]
			{
				"--base", "Samples/folder",
				"--template", "../FullMessage.cshtml",
			}));
		}

		[Fact]
		public void Precompile_Produces_Repeatable_Assembly_Bytes()
		{
			PrecompileTestCases.WithCache.Cleanup();
			try
			{
				string firstPath = Helper.RunCommandTrimNewline(
					"precompile", "-t", "Samples/FullMessage.cshtml", "-c", PrecompileTestCases.CACHE_DIR);
				byte[] first = File.ReadAllBytes(firstPath);

				PrecompileTestCases.WithCache.Cleanup();
				string secondPath = Helper.RunCommandTrimNewline(
					"precompile", "-t", "Samples/FullMessage.cshtml", "-c", PrecompileTestCases.CACHE_DIR);
				byte[] second = File.ReadAllBytes(secondPath);

				Assert.Equal(firstPath, secondPath);
				Assert.Equal(first, second);
			}
			finally
			{
				PrecompileTestCases.WithCache.Cleanup();
			}
		}

		public static string GetExpectedPrecompiledFilePath(string templateFilePath, TestScenario scenario)
		{
			var cacheDirectory = scenario.GetExpectedCacheDirectory(templateFilePath)
				?? throw new InvalidOperationException("The scenario has no expected cache directory.");
			var cacheFileInfo = scenario.ExpectedCachingStrategy.GetCachedFileInfo(scenario.GetTemplateKey(templateFilePath), templateFilePath, cacheDirectory);
			return scenario.GetExpectedPrecompiledFilePath(cacheFileInfo.AssemblyFilePath);
		}

		private static void Precompile(string templateFilePath, TestScenario scenario, string? expectedPrecompiledFilePath)
		{
			var commandLineArgs = new List<string>
			{
				"precompile",
				"-t",
				templateFilePath
			};
			commandLineArgs.AddRange(scenario.ExtraCommandLineArgs);

			var precompiledFilePath = Helper.RunCommandTrimNewline(commandLineArgs.ToArray());
			Assert.Equal(expectedPrecompiledFilePath, precompiledFilePath);
			Assert.True(File.Exists(precompiledFilePath));
		}
	}
}
