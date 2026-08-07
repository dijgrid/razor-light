using System.Threading.Tasks;
using Xunit;

namespace RazorLight.Tests.Snippets
{
	using Core;

	public class Snippets
	{
		public class ViewModel
		{
			public required string Name { get; set; }
		}

		[Fact]
		public async Task Simple()
		{
			#region simple
			var engine = new RazorLightEngineBuilder()
				.UseMemoryCachingProvider()
				.Build();

			string template = "Hello, @Model.Name. Welcome to RazorLight repository";
			ViewModel model = new ViewModel {Name = "John Doe"};

			string result = await engine.CompileRenderStringAsync("templateKey", template, model);

			#endregion

			Assert.Equal("Hello, John Doe. Welcome to RazorLight repository", result);
		}

		async Task RenderCompiledTemplate(RazorLightEngine engine, object model)
		{
			#region RenderCompiledTemplate
			var cacheResult = engine.Handler.Cache?.RetrieveTemplate("templateKey")
				?? throw new System.InvalidOperationException("Caching is not configured.");
			if(cacheResult.Success)
			{
				var templatePage = cacheResult.Template.TemplatePageFactory();
				string result = await engine.RenderTemplateAsync(templatePage, model);
			}
			#endregion
		}

		async Task FileSource()
		{
			#region FileSource
			var engine = new RazorLightEngineBuilder()
				.UseFileSystemProject("C:/RootFolder/With/YourTemplates")
				.UseMemoryCachingProvider()
				.Build();

			var model = new {Name = "John Doe"};
			string result = await engine.CompileRenderAsync("Subfolder/View.cshtml", model);

			#endregion
		}

		async Task EmbeddedResourceSource()
		{
			#region EmbeddedResourceSource
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(SomeService).Assembly)
				.UseMemoryCachingProvider()
				.Build();

			var model = new Model();
			string html = await engine.CompileRenderAsync("EmailTemplates.Body", model);

			#endregion
		}

		async Task EmbeddedResourceSourceWithRootNamespace()
		{
			#region EmbeddedResourceSourceWithRootNamespace
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(SomeService).Assembly, "Project.Core.EmailTemplates")
				.UseMemoryCachingProvider()
				.Build();

			var model = new Model();
			string html = await engine.CompileRenderAsync("Body", model);

			#endregion
		}

		public class Model
		{
		}
	}
}
namespace RazorLight.Tests.Snippets.Core
{
	public class SomeService
	{
	}
}
