using Mono.Cecil;
using RazorLight.Caching;
using Xunit;

namespace RazorLight.Precompile.Tests
{
	public class PrecompiledCachingProviderTests : IDisposable
	{
		private string _precompiledFilePath = null!;

		public PrecompiledCachingProviderTests()
		{
			PrecompileTestCases.CleanupDlls("Samples");
			_precompiledFilePath = Helper.RunCommandTrimNewline(
				"precompile",
				"-t",
				"folder/MessageItem.cshtml",
				"-b",
				"Samples");
		}

		public void Dispose()
		{
			PrecompileTestCases.CleanupDlls("Samples");
		}

		[Fact]
		public void CacheTemplate_And_Remove_Support_Runtime_Entries()
		{
			var cache = new PrecompiledCachingProvider(new[] { _precompiledFilePath }, null);
			var page = new TestPage();

			cache.CacheTemplate("runtime\\template.cshtml", () => page, null);

			Assert.True(cache.Contains("/runtime/template.cshtml"));
			Assert.True(cache.TryGetTemplate("runtime/template.cshtml", out var pageFactory));
			Assert.Same(page, pageFactory!());

			cache.Remove("runtime/template.cshtml");

			Assert.False(cache.Contains("/runtime/template.cshtml"));
			Assert.False(cache.TryGetTemplate("runtime/template.cshtml", out var missingFactory));
			Assert.Null(missingFactory);
		}

		[Fact]
		public void Precompiled_Keys_Normalize_Separators_And_Are_Case_Sensitive()
		{
			var cache = new PrecompiledCachingProvider(new[] { _precompiledFilePath }, null);

			Assert.True(cache.Contains("folder\\MessageItem.cshtml"));
			Assert.False(cache.Contains("folder/messageitem.cshtml"));

			cache.Remove("folder\\MessageItem.cshtml");

			Assert.False(cache.Contains("/folder/MessageItem.cshtml"));
		}

		[Fact]
		public void Map_Is_Immutable_And_Assembly_Diagnostics_Are_Preserved()
		{
			string invalidAssembly = Path.Combine(AppContext.BaseDirectory, "invalid-cache.dll");
			File.WriteAllText(invalidAssembly, "not an assembly");
			try
			{
				var cache = new PrecompiledCachingProvider(new[] { invalidAssembly, _precompiledFilePath }, null);

				Assert.Single(cache.Diagnostics);
				Assert.Contains("invalid-cache.dll", cache.Diagnostics[0]);
				Assert.Throws<NotSupportedException>(() =>
					((IDictionary<string, string>)cache.Map).Add("new", "value"));
			}
			finally
			{
				File.Delete(invalidAssembly);
			}
		}

		[Fact]
		public void Duplicate_Key_Diagnostic_Is_Deterministic()
		{
			string first = Path.Combine(AppContext.BaseDirectory, "a-duplicate.dll");
			string second = Path.Combine(AppContext.BaseDirectory, "z-duplicate.dll");
			File.Copy(_precompiledFilePath, first, overwrite: true);
			File.Copy(_precompiledFilePath, second, overwrite: true);
			try
			{
				RazorLightException exception = Assert.Throws<RazorLightException>(() =>
					new PrecompiledCachingProvider(new[] { second, first }, null))!;

				Assert.True(exception.Message.IndexOf(first, StringComparison.Ordinal) <
					exception.Message.IndexOf(second, StringComparison.Ordinal));
			}
			finally
			{
				File.Delete(first);
				File.Delete(second);
			}
		}

		[Fact]
		public void Rejects_Incompatible_Compiler_Metadata_With_Actionable_Diagnostic()
		{
			string incompatible = Path.Combine(AppContext.BaseDirectory, "incompatible-template.dll");
			using (var assembly = AssemblyDefinition.ReadAssembly(_precompiledFilePath))
			{
				CustomAttribute attribute = Assert.Single(assembly.CustomAttributes,
					item => item.AttributeType.FullName == "RazorLight.Razor.RazorLightTemplateAttribute");
				attribute.ConstructorArguments[3] = new CustomAttributeArgument(
					attribute.ConstructorArguments[3].Type,
					"incompatible-compiler");
				assembly.Write(incompatible);
			}

			try
			{
				var exception = Assert.Throws<RazorLightException>(() =>
					new PrecompiledCachingProvider(new[] { incompatible }, null));
				Assert.Contains("requires format", exception.Message);
				Assert.Contains("Recompile the template", exception.Message);
			}
			finally
			{
				File.Delete(incompatible);
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
