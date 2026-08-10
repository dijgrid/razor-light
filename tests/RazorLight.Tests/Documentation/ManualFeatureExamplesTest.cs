using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Html;
using RazorLight.Tests.Integration;
using RazorLight.Tests.Utils;
using Xunit;

namespace RazorLight.Tests.Documentation
{
	/// <summary>
	/// Executable examples for <c>docs/manual.md</c>. Each test is intentionally small and
	/// self-contained so it can be read as developer documentation as well as regression coverage.
	/// </summary>
	public sealed class ManualFeatureExamplesTest
	{
		[Fact]
		public async Task String_templates_support_typed_models_imports_and_view_bag()
		{
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDefaultNamespaces("System.Linq")
				.Build();

			dynamic viewBag = new ExpandoObject();
			viewBag.Heading = "Languages";
			var model = new FeatureModel(new[] { "C#", "Razor" });
			const string source = "@ViewBag.Heading: @string.Join(\", \", Model.Names.Select(x => x.ToUpperInvariant()))";

			string output = await engine.CompileRenderStringAsync(
				"typed-model",
				source,
				model,
				typeof(FeatureModel),
				(ExpandoObject)viewBag);

			Assert.Equal("Languages: C#, RAZOR", output);
		}

		[Fact]
		public async Task File_projects_enable_layouts_sections_and_includes()
		{
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseFileSystemProject(Path.Combine(DirectoryUtils.RootDirectory, "Assets", "Files"))
				.UseMemoryCachingProvider()
				.Build();

			var model = new TestViewModel { Name = "Ada" };
			string layoutOutput = await engine.CompileRenderAsync("template4", model);
			string includeOutput = await engine.CompileRenderAsync("template7", model);

			Assert.Contains("<layout>", layoutOutput, StringComparison.Ordinal);
			Assert.Contains("The content of the section", layoutOutput, StringComparison.Ordinal);
			Assert.Contains("included partial template", includeOutput, StringComparison.Ordinal);
		}

		[Fact]
		public async Task Reusable_templates_create_a_fresh_page_for_each_render()
		{
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.UseMemoryCachingProvider()
				.Build();

			await engine.CompileRenderStringAsync("greeting", "Hello @Model", "warmup");
			RazorLightTemplate template = await engine.CompileReusableTemplateAsync("greeting");

			string[] outputs = await Task.WhenAll(
				template.RenderAsync("Ada"),
				template.RenderAsync("Grace"));

			Assert.Equal(new[] { "Hello Ada", "Hello Grace" }, outputs);
			Assert.True(engine.IsTemplateCached("greeting"));
			engine.InvalidateTemplate("greeting");
			Assert.False(engine.IsTemplateCached("greeting"));
		}

		[Fact]
		public async Task Rendering_can_stream_to_a_caller_owned_writer()
		{
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.Build();

			await engine.CompileRenderStringAsync("writer", "Value: @Model", 0);
			ITemplatePage page = await engine.CompileTemplateAsync("writer");
			using var writer = new StringWriter();

			await engine.RenderTemplateAsync(page, 42, writer);

			Assert.Equal("Value: 42", writer.ToString());
		}

		[Fact]
		public async Task Plain_text_html_encoding_and_raw_output_are_explicit_policies()
		{
			await using IRazorLightEngine plain = new RazorLightEngineBuilder().UseNoProject().Build();
			await using IRazorLightEngine html = new RazorLightEngineBuilder().UseNoProject().UseHtmlEncoding().Build();

			string plainOutput = await plain.CompileRenderStringAsync("plain", "@Model", "<em>text</em>");
			string encodedOutput = await html.CompileRenderStringAsync("encoded", "@Model", "<em>text</em>");
			string rawOutput = await html.CompileRenderStringAsync("raw", "@Raw(Model)", "<em>text</em>");

			Assert.Equal("<em>text</em>", plainOutput);
			Assert.Equal("&lt;em&gt;text&lt;/em&gt;", encodedOutput);
			Assert.Equal("<em>text</em>", rawOutput);
		}

		[Fact]
		public async Task Trusted_c_sharp_source_can_supply_shared_template_helpers()
		{
			const string helper =
				"namespace ManualExamples; internal static class Words " +
				"{ internal static string Upper(string value) => value.ToUpperInvariant(); }";
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddCSharpSource("Words.cs", helper)
				.Build();

			string output = await engine.CompileRenderStringAsync(
				"source-composition",
				"@using ManualExamples\n@Words.Upper(Model)",
				"composed");

			Assert.Equal("COMPOSED", output.Trim());
		}

		[Fact]
		public async Task Page_initializers_run_for_every_created_page()
		{
			int initializedPages = 0;
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddPageInitializer(_ => Interlocked.Increment(ref initializedPages))
				.Build();

			await engine.CompileRenderStringAsync("initialized", "@Model", "first");
			await engine.CompileRenderAsync("initialized", "second");

			Assert.Equal(2, initializedPages);
		}

		[Fact]
		public async Task Cancellation_and_missing_template_errors_are_observable_contracts()
		{
			await using IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.EnableDebugMode()
				.Build();
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				engine.CompileRenderStringAsync("cancelled", "@Model", "value", cancellation.Token));
			await Assert.ThrowsAsync<TemplateNotFoundException>(() =>
				engine.CompileRenderAsync("missing", new object()));
		}

		/// <summary>A public model makes the type visible to the generated template assembly.</summary>
		public sealed record FeatureModel(IReadOnlyList<string> Names);
	}
}
