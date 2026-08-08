using NUnit.Framework;
using RazorLight.Caching;
using System.Runtime.InteropServices;

namespace RazorLight.Precompile.Tests
{
	public class FileSystemCachingStrategyTests
	{
		private static readonly object[] s_testCases = new object[]
		{
			FileHashCachingStrategy.Instance,
			SimpleFileCachingStrategy.Instance,
		};

		private static readonly string[] s_firstSepOptionsWindows = { "", "/", "\\" };
		private static readonly string[] s_secondSepOptionsWindows = { "/", "\\" };
		private static readonly string[] s_firstSepOptionsUnix = { "", "/" };
		private static readonly string[] s_secondSepOptionsUnix = { "/" };

		private static readonly IEnumerable<string[]> s_sepCombinations = GetSeparatorCombinations();

		private static IEnumerable<string[]> GetSeparatorCombinations()
		{
			string[] firstSepOptions, secondSepOptions;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				firstSepOptions = s_firstSepOptionsWindows;
				secondSepOptions = s_secondSepOptionsWindows;
			}
			else
			{
				firstSepOptions = s_firstSepOptionsUnix;
				secondSepOptions = s_secondSepOptionsUnix;
			}

			foreach (var s11 in firstSepOptions)
			{
				foreach (var s12 in firstSepOptions)
				{
					foreach (var s21 in secondSepOptions)
					{
						foreach (var s22 in secondSepOptions)
						{
							if (s11 != s12 || s21 != s22)
							{
								yield return new[] { s11, s21, s12, s22 };
							}
						}
					}
				}
			}
		}

		[TestCaseSource(nameof(s_testCases))]
		public void DifferentKey(IFileSystemCachingStrategy s)
		{
			var templateFilePath = "Samples/folder/MessageItem.cshtml";
			var o1 = s.GetCachedFileInfo("folder/MessageItem.cshtml", templateFilePath, "X:/");
			var o2 = s.GetCachedFileInfo("MessageItem.cshtml", templateFilePath, "X:/");
			Assert.AreNotEqual(o1.AssemblyFilePath, o2.AssemblyFilePath);
		}

		[TestCaseSource(nameof(s_sepCombinations))]
		public void EquivalentKeyFileHashCachingStrategy(string[] sepCombination)
		{
			var (asmFilePath1, asmFilePath2) = GetAsmFilePaths(FileHashCachingStrategy.Instance, sepCombination);
			Assert.AreEqual(asmFilePath1, asmFilePath2);
		}

		[TestCaseSource(nameof(s_sepCombinations))]
		public void EquivalentKeySimpleFileCachingStrategy(string[] sepCombination)
		{
			var (asmFilePath1, asmFilePath2) = GetAsmFilePaths(SimpleFileCachingStrategy.Instance, sepCombination);
			if (asmFilePath1 != asmFilePath2)
			{
				asmFilePath1 = Path.GetFullPath(asmFilePath1);
				asmFilePath2 = Path.GetFullPath(asmFilePath2);
			}
			Assert.AreEqual(asmFilePath1, asmFilePath2);
		}

		[TestCase("../outside")]
		[TestCase("nested/../../outside")]
		[TestCase("..\\outside")]
		public void SimpleStrategy_Rejects_Keys_Outside_Cache_Root(string key)
		{
			string cacheRoot = Path.Combine(TestContext.CurrentContext.WorkDirectory, "cache-root");

			Assert.Throws<InvalidOperationException>(() =>
				SimpleFileCachingStrategy.Instance.GetCachedFileInfo(key, "template.cshtml", cacheRoot));
		}

		[Test]
		public void FileHash_Changes_With_Dependencies_And_Is_Stable_When_Inputs_Are_Unchanged()
		{
			string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				string templatePath = Path.Combine(root, "template.cshtml");
				string sourcePath = Path.Combine(root, "Shared.cs");
				string importsPath = Path.Combine(root, "_ViewImports.cshtml");
				File.WriteAllText(templatePath, "@Model");
				File.WriteAllText(sourcePath, "public static class Shared { public const int Value = 1; }");
				File.WriteAllText(importsPath, "@using System");

				string first = FileHashCachingStrategy.Instance
					.GetCachedFileInfo("template.cshtml", templatePath, root).AssemblyFilePath;
				string repeat = FileHashCachingStrategy.Instance
					.GetCachedFileInfo("template.cshtml", templatePath, root).AssemblyFilePath;
				File.WriteAllText(sourcePath, "public static class Shared { public const int Value = 2; }");
				string changed = FileHashCachingStrategy.Instance
					.GetCachedFileInfo("template.cshtml", templatePath, root).AssemblyFilePath;
				File.WriteAllText(sourcePath, "public static class Shared { public const int Value = 1; }");
				File.WriteAllText(importsPath, "@using System.Linq");
				string importsChanged = FileHashCachingStrategy.Instance
					.GetCachedFileInfo("template.cshtml", templatePath, root).AssemblyFilePath;

				Assert.That(repeat, Is.EqualTo(first));
				Assert.That(changed, Is.Not.EqualTo(first));
				Assert.That(importsChanged, Is.Not.EqualTo(first));
				Assert.That(Path.GetFileNameWithoutExtension(first), Has.Length.EqualTo(64));
			}
			finally
			{
				Directory.Delete(root, recursive: true);
			}
		}

		[Test]
		public void FileSystemProvider_Missing_Source_Is_A_Normal_Cache_Miss()
		{
			string root = Path.Combine(TestContext.CurrentContext.WorkDirectory, Guid.NewGuid().ToString("N"));
			string cacheRoot = Path.Combine(root, "cache");
			Directory.CreateDirectory(cacheRoot);
			try
			{
				using var provider = new FileSystemCachingProvider(root, cacheRoot, FileHashCachingStrategy.Instance);

				Assert.That(provider.Contains("missing.cshtml"), Is.False);
				Assert.That(provider.TryGetTemplate("missing.cshtml", out var factory), Is.False);
				Assert.That((object?)factory, Is.Null);
				Assert.DoesNotThrow(() => provider.Remove("missing.cshtml"));
			}
			finally
			{
				Directory.Delete(root, recursive: true);
			}
		}

		private static (string, string) GetAsmFilePaths(IFileSystemCachingStrategy s, string[] sepCombination)
		{
			var templateFilePath = "Samples/folder/MessageItem.cshtml";
			string key1 = $"{sepCombination[0]}folder{sepCombination[1]}MessageItem.cshtml";
			string key2 = $"{sepCombination[2]}folder{sepCombination[3]}MessageItem.cshtml";
			Assert.AreNotEqual(key1, key2);
			var asmFilePath1 = s.GetCachedFileInfo(key1, templateFilePath, "X:/").AssemblyFilePath;
			var asmFilePath2 = s.GetCachedFileInfo(key2, templateFilePath, "X:/").AssemblyFilePath;
			return (asmFilePath1, asmFilePath2);
		}
	}
}
