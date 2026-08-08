using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.DependencyInjection;
using RazorLight.Tests.Models;
using Xunit;

namespace RazorLight.Tests.DependencyInjection
{
	public class PropertyInjectorTest
	{
		[Fact]
		public void Throws_On_Null_Services()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new PropertyInjector().Inject(TemplatePageTest.CreatePage(_ => { }), null!));
		}

		[Fact]
		public async Task Ensure_Registered_Properties_Are_Injected()
		{
			var collection = new ServiceCollection();
			string expectedValue = "TestValue";
			string templateKey = "key";
			collection.AddSingleton(new TestViewModel { Title = expectedValue });
			var services = collection.BuildServiceProvider();
			var propertyInjector = new PropertyInjector();

			var builder = new StringBuilder();
			builder.AppendLine("@model object");
			builder.AppendLine("@inject RazorLight.Tests.Models.TestViewModel test");
			builder.AppendLine("Hello @test");

			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root))
				.SetOperatingAssembly(typeof(Root).Assembly)
				.AddDynamicTemplates(new Dictionary<string, string> { { templateKey, builder.ToString() } })
				.Build();

			ITemplatePage templatePage = await engine.CompileTemplateAsync(templateKey);

			//Act
			propertyInjector.Inject(templatePage, services);

			//Assert
			var property = templatePage.GetType().GetProperty("test");
			Assert.NotNull(property);
			var prop = property.GetValue(templatePage);

			Assert.NotNull(prop);
			var model = Assert.IsAssignableFrom<TestViewModel>(prop);
			Assert.Equal(model.Title, expectedValue);
		}

		[Fact]
		public void Reuses_One_Injection_Plan_Per_Page_Type()
		{
			using var services = new ServiceCollection().BuildServiceProvider();
			var injector = new PropertyInjector();

			injector.Inject(TemplatePageTest.CreatePage(_ => { }), services);
			injector.Inject(TemplatePageTest.CreatePage(_ => { }), services);

			Assert.Equal(1, injector.PlanCreationCount);
		}
	}
}
