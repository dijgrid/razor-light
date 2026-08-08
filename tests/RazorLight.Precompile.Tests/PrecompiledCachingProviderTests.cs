using NUnit.Framework;

namespace RazorLight.Precompile.Tests
{
	[NonParallelizable]
	public class PrecompiledCachingProviderTests
	{
		private string _precompiledFilePath = null!;

		[SetUp]
		public void SetUp()
		{
			PrecompileTestCases.CleanupDlls("Samples");
			_precompiledFilePath = Helper.RunCommandTrimNewline(
				"precompile",
				"-t",
				"folder/MessageItem.cshtml",
				"-b",
				"Samples");
		}

		[TearDown]
		public void TearDown()
		{
			PrecompileTestCases.CleanupDlls("Samples");
		}

		[Test]
		public void CacheTemplate_And_Remove_Support_Runtime_Entries()
		{
			var cache = new PrecompiledCachingProvider(new[] { _precompiledFilePath }, null);
			var page = new TestPage();

			cache.CacheTemplate("runtime\\template.cshtml", () => page, null);

			Assert.That(cache.Contains("/runtime/template.cshtml"), Is.True);
			Assert.That(cache.TryGetTemplate("runtime/template.cshtml", out var pageFactory), Is.True);
			Assert.That(pageFactory!(), Is.SameAs(page));

			cache.Remove("runtime/template.cshtml");

			Assert.That(cache.Contains("/runtime/template.cshtml"), Is.False);
			Assert.That(cache.TryGetTemplate("runtime/template.cshtml", out var missingFactory), Is.False);
			Assert.That((object?)missingFactory, Is.Null);
		}

		[Test]
		public void Precompiled_Keys_Normalize_Separators_And_Are_Case_Sensitive()
		{
			var cache = new PrecompiledCachingProvider(new[] { _precompiledFilePath }, null);

			Assert.That(cache.Contains("folder\\MessageItem.cshtml"), Is.True);
			Assert.That(cache.Contains("folder/messageitem.cshtml"), Is.False);

			cache.Remove("folder\\MessageItem.cshtml");

			Assert.That(cache.Contains("/folder/MessageItem.cshtml"), Is.False);
		}

		[Test]
		public void Map_Is_Immutable_And_Assembly_Diagnostics_Are_Preserved()
		{
			string invalidAssembly = Path.Combine(TestContext.CurrentContext.WorkDirectory, "invalid-cache.dll");
			File.WriteAllText(invalidAssembly, "not an assembly");
			try
			{
				var cache = new PrecompiledCachingProvider(new[] { invalidAssembly, _precompiledFilePath }, null);

				Assert.That(cache.Diagnostics, Has.Count.EqualTo(1));
				Assert.That(cache.Diagnostics[0], Does.Contain("invalid-cache.dll"));
				Assert.Throws<NotSupportedException>(() =>
					((IDictionary<string, string>)cache.Map).Add("new", "value"));
			}
			finally
			{
				File.Delete(invalidAssembly);
			}
		}

		[Test]
		public void Duplicate_Key_Diagnostic_Is_Deterministic()
		{
			string first = Path.Combine(TestContext.CurrentContext.WorkDirectory, "a-duplicate.dll");
			string second = Path.Combine(TestContext.CurrentContext.WorkDirectory, "z-duplicate.dll");
			File.Copy(_precompiledFilePath, first, overwrite: true);
			File.Copy(_precompiledFilePath, second, overwrite: true);
			try
			{
				RazorLightException exception = Assert.Throws<RazorLightException>(() =>
					new PrecompiledCachingProvider(new[] { second, first }, null))!;

				Assert.That(exception.Message.IndexOf(first, StringComparison.Ordinal),
					Is.LessThan(exception.Message.IndexOf(second, StringComparison.Ordinal)));
			}
			finally
			{
				File.Delete(first);
				File.Delete(second);
			}
		}

		private sealed class TestPage : TemplatePage
		{
			public override Task ExecuteAsync() => Task.CompletedTask;

			public override void SetModel(object? model)
			{
			}
		}
	}
}
