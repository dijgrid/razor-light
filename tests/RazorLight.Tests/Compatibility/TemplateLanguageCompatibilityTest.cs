using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;
using RazorLight.Tests.Integration;
using RazorLight.Tests.Utils;
using Xunit;

namespace RazorLight.Tests.Compatibility
{
	[Collection(NonParallelRazorCompilationCollection.Name)]
	public class TemplateLanguageCompatibilityTest
	{
		private const string FullyQualifiedModel = "RazorLight.Tests.Compatibility.CompatibilityModel";

		public static IEnumerable<object?[]> SourceAndImportCases()
		{
			yield return new object?[] { "string", "explicit-using", null };
			yield return new object?[] { "string", "no-import", "does not contain a definition for 'Any'" };
			yield return new object?[] { "string", "configured-namespace", "type or namespace name 'CompatibilityModel' could not be found" };
			yield return new object?[] { "file", "built-in", null };
			yield return new object?[] { "embedded-resource", "built-in", null };
			yield return new object?[] { "custom-project", "built-in", null };
			yield return new object?[] { "custom-project", "configured-namespace", null };
		}

		[Theory]
		[MemberData(nameof(SourceAndImportCases))]
		public async Task Characterizes_Source_And_Import_Matrix(
			string source,
			string import,
			string? expectedDiagnostic)
		{
			if (expectedDiagnostic == null)
			{
				string rendered = await RenderMatrixCaseAsync(source, import);

				Assert.Equal("True|BB", rendered.Trim());
				return;
			}

			var exception = await Assert.ThrowsAsync<TemplateCompilationException>(
				() => RenderMatrixCaseAsync(source, import));

			Assert.Contains(
				exception.CompilationDiagnostics,
				diagnostic => diagnostic.ErrorMessage.Contains(expectedDiagnostic));
		}

		public static IEnumerable<object[]> ModelCases()
		{
			yield return new object[] { "explicit-strong-model" };
			yield return new object[] { "generic-call-without-model-directive" };
			yield return new object[] { "anonymous-object" };
			yield return new object[] { "expando-object" };
			yield return new object[] { "dynamic-receiver" };
		}

		[Theory]
		[MemberData(nameof(ModelCases))]
		public async Task Characterizes_Model_Forms(string modelCase)
		{
			var engine = NewStringEngine();
			string key = $"model-{modelCase}";
			string template = "@Model.Items[0]";
			string rendered;

			switch (modelCase)
			{
				case "explicit-strong-model":
					template = $"@model {FullyQualifiedModel}\n@Model.Items[0]";
					rendered = await engine.CompileRenderStringAsync(key, template, NewModel());
					break;
				case "generic-call-without-model-directive":
					rendered = await engine.CompileRenderStringAsync(key, template, NewModel());
					break;
				case "anonymous-object":
					rendered = await engine.CompileRenderStringAsync(
						key,
						template,
						new { Items = new[] { "a", "bb" } });
					break;
				case "expando-object":
					var expando = NewExpandoModel();
					rendered = await engine.CompileRenderStringAsync(key, template, expando);
					break;
				case "dynamic-receiver":
					dynamic dynamicModel = NewExpandoModel();
					rendered = await RenderDynamicAsync(engine, key, template, dynamicModel);
					break;
				default:
					throw new Xunit.Sdk.XunitException($"Unknown model case: {modelCase}");
			}

			Assert.Equal("a", rendered.Trim());
		}

		[Fact]
		public async Task Captures_Dynamic_Lambda_Extension_Method_Diagnostic()
		{
			var engine = NewStringEngine();
			string template =
				"@using System.Linq\n@(Model.Items.Where(item => item.Length > 1).FirstOrDefault())";

			var exception = await Assert.ThrowsAsync<TemplateCompilationException>(() =>
				engine.CompileRenderStringAsync("dynamic-lambda", template, NewExpandoModel()));

			Assert.Contains(
				exception.CompilationDiagnostics,
				diagnostic => diagnostic.ErrorMessage.Contains(
					"Cannot use a lambda expression as an argument to a dynamically dispatched operation"));
		}

		[Fact]
		public async Task Generated_Source_Records_The_Dynamic_Model_And_Lambda_Call()
		{
			var generator = new RazorSourceGenerator(DefaultRazorEngine.Instance, new NoRazorProject());
			var item = new TextSourceRazorProjectItem(
				"generated-source",
				"@using System.Linq\n@(Model.Items.Where(item => item.Length > 1).FirstOrDefault())");

			IGeneratedRazorTemplate generated = await generator.GenerateCodeAsync(item);

			Assert.Contains("TemplatePage<dynamic>", generated.GeneratedCode);
			Assert.Contains("Model.Items.Where(item => item.Length > 1).FirstOrDefault()", generated.GeneratedCode);
		}

		[Fact]
		public async Task Reused_String_Key_Keeps_First_Content_Model_Type_And_Imports()
		{
			var contentEngine = NewStringEngine();
			Assert.Equal(
				"first",
				await contentEngine.CompileRenderStringAsync("same-content", "first", NewModel()));
			Assert.Equal(
				"first",
				await contentEngine.CompileRenderStringAsync("same-content", "second", NewModel()));

			var modelEngine = NewStringEngine();
			string firstModelTemplate = $"@model {FullyQualifiedModel}\n@Model.Items[0]";
			Assert.Equal(
				"a",
				(await modelEngine.CompileRenderStringAsync(
					"same-model",
					firstModelTemplate,
					NewModel())).Trim());
			await Assert.ThrowsAsync<System.InvalidCastException>(() =>
				modelEngine.CompileRenderStringAsync(
					"same-model",
					"@model RazorLight.Tests.Compatibility.AlternateCompatibilityModel\n@Model.Value",
					new AlternateCompatibilityModel { Value = "alternate" }));

			var importEngine = NewStringEngine();
			string withImport = $"@using System.Linq\n@model {FullyQualifiedModel}\n@(Model.Items.Any())";
			Assert.Equal(
				"True",
				(await importEngine.CompileRenderStringAsync(
					"same-imports",
					withImport,
					NewModel())).Trim());
			Assert.Equal(
				"True",
				(await importEngine.CompileRenderStringAsync(
					"same-imports",
					$"@model {FullyQualifiedModel}\nmissing-import:@(Model.Items.Any())",
					NewModel())).Trim());
		}

		private static async Task<string> RenderMatrixCaseAsync(string source, string import)
		{
			var model = NewModel();

			switch (source)
			{
				case "string":
					var stringBuilder = new RazorLightEngineBuilder().UseNoProject();
					string stringTemplate;
					if (import == "explicit-using")
					{
						stringTemplate = MatrixTemplate(FullyQualifiedModel, "@using System.Linq\n");
					}
					else if (import == "configured-namespace")
					{
						stringBuilder.AddDefaultNamespaces("RazorLight.Tests.Compatibility");
						stringTemplate = MatrixTemplate("CompatibilityModel", "@using System.Linq\n");
					}
					else
					{
						stringTemplate = MatrixTemplate(FullyQualifiedModel, string.Empty);
					}

					return await stringBuilder.Build().CompileRenderStringAsync(
						$"matrix-string-{import}",
						stringTemplate,
						model);
				case "file":
					return await new RazorLightEngineBuilder()
						.UseFileSystemProject(Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files"))
						.Build()
						.CompileRenderAsync("CompatibilityMatrix.cshtml", model);
				case "embedded-resource":
					return await new RazorLightEngineBuilder()
						.UseEmbeddedResourcesProject(
							typeof(Root).Assembly,
							"RazorLight.Tests.Assets.Embedded")
						.Build()
						.CompileRenderAsync("CompatibilityMatrix", model);
				case "custom-project":
					string customModelName = import == "configured-namespace"
						? "CompatibilityModel"
						: FullyQualifiedModel;
					var project = new InMemoryRazorProject(
						"matrix",
						MatrixTemplate(customModelName, string.Empty));
					var customBuilder = new RazorLightEngineBuilder().UseProject(project);
					if (import == "configured-namespace")
					{
						customBuilder.AddDefaultNamespaces("RazorLight.Tests.Compatibility");
					}

					return await customBuilder.Build().CompileRenderAsync("matrix", model);
				default:
					throw new Xunit.Sdk.XunitException($"Unknown source case: {source}");
			}
		}

		private static string MatrixTemplate(string modelType, string imports)
		{
			return imports
				+ $"@model {modelType}\n"
				+ "@{ var filtered = Model.Items.Where(item => item.Length > 1).Select(item => item.ToUpperInvariant()); }\n"
				+ "@(Model.Items.Any())|@(filtered.FirstOrDefault())";
		}

		private static RazorLightEngine NewStringEngine()
		{
			return new RazorLightEngineBuilder()
				.UseNoProject()
				.Build();
		}

		private static CompatibilityModel NewModel()
		{
			return new CompatibilityModel { Items = new[] { "a", "bb" } };
		}

		private static ExpandoObject NewExpandoModel()
		{
			dynamic model = new ExpandoObject();
			model.Items = new[] { "a", "bb" };
			return model;
		}

		private static Task<string> RenderDynamicAsync(
			RazorLightEngine engine,
			string key,
			string template,
			dynamic model)
		{
			return engine.CompileRenderStringAsync(key, template, model);
		}

		private sealed class InMemoryRazorProject : RazorLightProject
		{
			private readonly RazorLightProjectItem item;

			public InMemoryRazorProject(string key, string content)
			{
				item = new InMemoryRazorProjectItem(key, content);
			}

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			{
				return Task.FromResult(item);
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			{
				return Task.FromResult(Enumerable.Empty<RazorLightProjectItem>());
			}
		}

		private sealed class InMemoryRazorProjectItem : RazorLightProjectItem
		{
			private readonly byte[] content;

			public InMemoryRazorProjectItem(string key, string content)
			{
				Key = key;
				this.content = Encoding.UTF8.GetBytes(content);
			}

			public override string Key { get; }

			public override bool Exists => true;

			public override Stream Read()
			{
				return new MemoryStream(content);
			}
		}
	}

	public class CompatibilityModel
	{
		public required string[] Items { get; set; }
	}

	public class AlternateCompatibilityModel
	{
		public required string Value { get; set; }
	}
}
