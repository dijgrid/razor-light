using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RazorLight.Compilation;
using RazorLight.Razor;
using RazorLight.Tests.Utils;
using Xunit;

namespace RazorLight.Tests
{
	public class CSharpSourceCompositionTest
	{
		[Fact]
		public async Task Global_InMemory_Source_Is_Compiled_With_String_Template()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddCSharpSource(
					"Shared/Words.cs",
					"using System; namespace Composition; internal static class Words { private static readonly Func<string, string> Transform = value => value.ToUpperInvariant(); internal static string Upper(string value) => Transform(value); }")
				.Build();

			string result = await engine.CompileRenderStringAsync(
				"global-source",
				"@using Composition\n@Words.Upper(Model)",
				"mixed");

			Assert.Equal("MIXED", result);
		}

		[Fact]
		public async Task Directive_Resolves_A_Source_Relative_To_The_Template()
		{
			var engine = new RazorLightEngineBuilder()
				.UseFileSystemProject(DirectoryUtils.RootDirectory)
				.EnableDebugMode()
				.Build();

			string result = await engine.CompileRenderAsync(
				"Assets/Files/CSharpSource/Templates/Greeting",
				"RazorLight");

			Assert.Equal("Hello, RazorLight!", result.Trim());
		}

		[Theory]
		[InlineData("../outside.cs")]
		[InlineData("helper.txt")]
		public void Unsafe_Or_NonCSharp_Source_Paths_Are_Rejected(string sourceKey)
		{
			Assert.Throws<InvalidOperationException>(() =>
				RazorLight.Generation.CSharpSourceResolver.Normalize(sourceKey, "template.cshtml"));
		}

		[Fact]
		public async Task Imported_Source_Diagnostics_Use_The_Logical_Source_Path()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.EnableDebugMode()
				.AddCSharpSource("Shared/Broken.cs", "namespace Composition; internal static class Broken { nope }")
				.Build();

			var exception = await Assert.ThrowsAsync<TemplateCompilationException>(() =>
				engine.CompileRenderStringAsync<object?>("broken-source", "text", null));

			Assert.Contains(exception.CompilationDiagnostics, diagnostic =>
				diagnostic.LineSpan?.Path == "Shared/Broken.cs");
		}

		[Fact]
		public async Task Directive_Resolves_Embedded_CSharp_Source()
		{
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root).Assembly, "RazorLight.Tests.Assets.Embedded")
				.Build();

			string result = await engine.CompileRenderAsync("CSharpComposition", "abc");

			Assert.Equal("cba", result.Trim());
		}

		[Fact]
		public async Task TopLevel_Statements_Are_Rejected_As_Normal_Compiler_Errors()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.EnableDebugMode()
				.AddCSharpSource("Shared/Script.cs", "System.Console.WriteLine(\"not a compilation unit\");")
				.Build();

			var exception = await Assert.ThrowsAsync<TemplateCompilationException>(() =>
				engine.CompileRenderStringAsync<object?>("top-level-source", "text", null));

			Assert.Contains(exception.CompilationDiagnostics, diagnostic =>
				diagnostic.ErrorMessage.Contains("top-level statements", StringComparison.OrdinalIgnoreCase));
		}

		[Fact]
		public async Task Source_Change_Token_Invalidates_Dependent_Template_Caches()
		{
			var project = new ChangeableSourceProject();
			var engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.UseMemoryCachingProvider()
				.Build();

			Assert.Equal("one", (await engine.CompileRenderAsync<object?>("template", null)).Trim());

			project.UpdateSource("namespace Composition; internal static class Values { internal static string Current => \"two\"; }");

			Assert.Equal("two", (await engine.CompileRenderAsync<object?>("template", null)).Trim());
		}

		private sealed class ChangeableSourceProject : RazorLightProject
		{
			private const string Template = "@compileSource \"/Shared.cs\"\n@using Composition\n@Values.Current";
			private string source = "namespace Composition; internal static class Values { internal static string Current => \"one\"; }";
			private CancellationTokenSource change = new CancellationTokenSource();

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) =>
				Task.FromResult<RazorLightProjectItem>(new TextSourceRazorProjectItem(templateKey, Template));

			public override Task<RazorLightProjectItem> GetSourceItemAsync(string sourceKey)
			{
				var item = new TextSourceRazorProjectItem(sourceKey, source)
				{
					ExpirationToken = new CancellationChangeToken(change.Token)
				};
				return Task.FromResult<RazorLightProjectItem>(item);
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
				Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());

			public void UpdateSource(string value)
			{
				source = value;
				CancellationTokenSource previous = change;
				change = new CancellationTokenSource();
				previous.Cancel();
				previous.Dispose();
			}
		}
	}
}
