using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using RazorLight.Compilation;
using RazorLight.Html;
using RazorLight.Razor;
using RazorLight.Tests.Razor;
using RazorLight.Tests.Utils;
using Xunit;

namespace RazorLight.Tests
{
	public class RazorLightEngineTest
	{
		[Fact]
		public async Task Html_Encoding_Is_Opt_In()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.UseHtmlEncoding()
				.Build();

			var result = await engine.CompileRenderStringAsync(
				"html-encoding",
				"@Model",
				"<script>");

			Assert.Equal("&lt;script&gt;", result);
		}

		[Fact]
		public async Task PlainText_Default_Renders_Models_Without_Escaping()
		{
			//Arrange
			var engine = new RazorLightEngineBuilder()
				.UseMemoryCachingProvider()
				.UseFileSystemProject(DirectoryUtils.RootDirectory)
				.Build();

			string key = "key";
			string content = "@Model.Entity";

			var model = new { Entity = "<pre></pre>" };

			// act
			var result = await engine.CompileRenderStringAsync(key, content, model);

			// assert
			Assert.Contains("<pre></pre>", result);
		}

		[Fact]
		public async Task Ensure_QuickStart_Demo_Code_Works()
		{
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root))
				.UseMemoryCachingProvider()
				.Build();

			string template = "Hello, @Model.Name. Welcome to RazorLight repository";
			var model = new { Name = "John Doe" };

			string result = await engine.CompileRenderStringAsync("templateKey", template, model);
			Assert.Equal("Hello, John Doe. Welcome to RazorLight repository", result);
		}

		[Fact]
		public async Task Ensure_Content_Added_To_DynamicTemplates()
		{
			var options = new RazorLightOptions();

			const string key = "key";
			const string content = "content";
			var project = new TestRazorProject { Value = new TextSourceRazorProjectItem(key, content) };

			var engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.UseOptions(options)
				.AddDynamicTemplates(new Dictionary<string, string>
				{
					[key] = content,
				})
				.Build();

			var actual = await engine.CompileRenderStringAsync(key, content, new object(), new ExpandoObject());

			Assert.Empty(options.DynamicTemplates);
			Assert.Equal(content, actual);
		}

		[Fact]
		public async Task Ensure_Content_Added_To_DynamicTemplates_When_Options_Not_Set_Explicitly()
		{
			const string key = "key";
			const string content = "content";
			var project = new NoRazorProject();

			var engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.AddDynamicTemplates(new Dictionary<string, string>
				{
					[key] = content,
				})
				.Build();

			var actual = await engine.CompileRenderStringAsync(key, content, new object(), new ExpandoObject());

			Assert.Equal(content, actual);
		}

		[Fact]
		public async Task Ensure_Content_Added_To_DynamicTemplates_When_Both_RazorLightProject_And_Options_Not_Set_Explicitly()
		{
			const string key = "key";
			const string content = "content";

			var engine = new RazorLightEngineBuilder()
				.Build();

			var actual = await engine.CompileRenderStringAsync(key, content, new object(), new ExpandoObject());

			Assert.Equal(content, actual);
		}

		[Fact]
		public async Task Ensure_Content_Added_To_DynamicTemplates_When_RazorLightProject_Set_Explicitly_And_Options_Not_Set_Explicitly()
		{
			const string key = "key";
			const string content = "content";

			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.Build();

			var actual = await engine.CompileRenderStringAsync(key, content, new object(), new ExpandoObject());

			Assert.Equal(content, actual);
		}
	}
}
