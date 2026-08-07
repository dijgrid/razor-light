using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RazorLight.Compilation;
using RazorLight.Compatibility;
using RazorLight.Generation;
using RazorLight.Razor;
using RazorLight.Tests.Integration;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	[Collection(NonParallelRazorCompilationCollection.Name)]
	public class CurrentCompilerCompatibilityTest
	{
		public static IEnumerable<object[]> CurrentCSharpSyntaxCases()
		{
			yield return new object[]
			{
				"collection-expression",
				"@{ string[] values = [\"alpha\", \"beta\"]; }@(values.Length)",
				"2"
			};
			yield return new object[]
			{
				"raw-string",
				"@{ var value = \"\"\"raw value\"\"\"; }@value",
				"raw value"
			};
			yield return new object[]
			{
				"pattern-matching",
				"@{ object value = new[] { 1, 2 }; }@(value is int[] { Length: > 1 })",
				"True"
			};
			yield return new object[]
			{
				"nullable-directive",
				"@functions { #nullable enable\nprivate string? Normalize(string? value) => value;\n}\n@(Normalize(null) ?? \"none\")",
				"none"
			};
			yield return new object[]
			{
				"async-code",
				"@{ await Task.Yield(); }complete",
				"complete"
			};
		}

		[Theory]
		[MemberData(nameof(CurrentCSharpSyntaxCases))]
		public async Task Renders_Current_CSharp_Syntax(
			string caseName,
			string template,
			string expected)
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();

			string rendered = await engine.CompileRenderStringAsync<object?>(caseName, template, model: null);

			Assert.Equal(expected, rendered.Trim());
		}

		[Fact]
		public async Task Generated_Code_Baseline_Covers_Representative_Razor_Directives()
		{
			const string template =
				"@using System.Globalization\n" +
				"@model RazorLight.Tests.Compatibility.CompatibilityModel\n" +
				"@inject RazorLight.Tests.Models.TestViewModel Service\n" +
				"@functions { private string Format(string value) => value.ToUpperInvariant(); }\n" +
				"@Format(Model.Items[0])";
			var generator = new RazorSourceGenerator(
				Razor6CompilerCompatibility.CreateEngine(),
				new NoRazorProject(),
				namespaces: null,
				includeDetailedDiagnostics: true);

			IGeneratedRazorTemplate generated = await generator.GenerateCodeAsync(
				new TextSourceRazorProjectItem("directive-baseline", template));

			Assert.Contains("using System.Globalization;", generated.GeneratedCode);
			Assert.Contains("TemplatePage<RazorLight.Tests.Compatibility.CompatibilityModel>", generated.GeneratedCode);
			Assert.Contains("public RazorLight.Tests.Models.TestViewModel Service", generated.GeneratedCode);
			Assert.Contains("private string Format(string value) => value.ToUpperInvariant();", generated.GeneratedCode);
			Assert.Contains("public async override global::System.Threading.Tasks.Task ExecuteAsync()", generated.GeneratedCode);
		}

		[Fact]
		public async Task Razor_Diagnostic_Baseline_Reports_Malformed_Model_Directive()
		{
			var generator = new RazorSourceGenerator(
				Razor6CompilerCompatibility.CreateEngine(),
				new NoRazorProject(),
				namespaces: null,
				includeDetailedDiagnostics: true);
			var item = new TextSourceRazorProjectItem("diagnostic-baseline", "@model\ncontent");

			var exception = await Assert.ThrowsAsync<TemplateGenerationException>(
				() => generator.GenerateCodeAsync(item));

			var diagnostic = Assert.Single(exception.Diagnostics);
			Assert.Equal("RZ1013", diagnostic.Id);
			Assert.Equal("The 'model' directive expects a type name.", diagnostic.GetMessage());
		}

		[Fact]
		public async Task Razor_Diagnostics_RedactTemplateDetailsByDefault()
		{
			const string privatePath = "C:/private/templates/customer.cshtml";
			var generator = new RazorSourceGenerator(Razor6CompilerCompatibility.CreateEngine(), new NoRazorProject());
			var item = new TextSourceRazorProjectItem(privatePath, "@model\ncontent");

			var exception = await Assert.ThrowsAsync<TemplateGenerationException>(
				() => generator.GenerateCodeAsync(item));

			Assert.Empty(exception.Diagnostics);
			Assert.Contains("RZ1013", exception.Message, StringComparison.Ordinal);
			Assert.DoesNotContain(privatePath, exception.Message, StringComparison.Ordinal);
			Assert.DoesNotContain("expects a type name", exception.Message, StringComparison.Ordinal);
		}
	}
}
