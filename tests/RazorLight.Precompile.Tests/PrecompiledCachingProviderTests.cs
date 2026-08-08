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
			Assert.Throws<RazorLightException>(() => cache.TryGetTemplate("runtime/template.cshtml", out _));
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

		private sealed class TestPage : TemplatePage
		{
			public override Task ExecuteAsync() => Task.CompletedTask;

			public override void SetModel(object? model)
			{
			}
		}
	}
}
