using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Text;
using Xunit;

namespace RazorLight.Tests
{
	public class RazorLightEngineBuilderTest
	{
		[Fact]
		public void Throws_On_Null_Project()
		{
			Action action = () => new RazorLightEngineBuilder().UseProject(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_RootType_ForEmbeddedProject()
		{
			Action action = () => new RazorLightEngineBuilder().UseEmbeddedResourcesProject((Type)null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_RootPath_ForFilesystemProject()
		{
			Action action = () => new RazorLightEngineBuilder().UseFileSystemProject(null!);

			Assert.Throws<DirectoryNotFoundException>(action);
		}


		[Fact]
		public void Throws_On_Null_CachingProvider()
		{
			Action action = () => new RazorLightEngineBuilder().UseCachingProvider(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_Namespaces()
		{
			Action action = () => new RazorLightEngineBuilder().AddDefaultNamespaces(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_AddMetadataReferences()
		{
			Action action = () => new RazorLightEngineBuilder().AddMetadataReferences(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_IncludeAssemblies()
		{
			Action action = () => new RazorLightEngineBuilder().IncludeAssemblies(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_DynamicTemplates()
		{
			Action action = () => new RazorLightEngineBuilder().AddDynamicTemplates(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_PrerenderCallbacks()
		{
			Action action = () => new RazorLightEngineBuilder().AddPrerenderCallbacks(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Throws_On_Null_Assembly()
		{
			Action action = () => new RazorLightEngineBuilder().SetOperatingAssembly(null!);

			Assert.Throws<ArgumentNullException>(action);
		}

		[Fact]
		public void Respects_Options_Passed_In()
		{

		}

		[Fact]
		public void Throws_On_Conflicting_Options_Configurations()
		{
			Func<RazorLightEngineBuilder> GetEngine = () => new RazorLightEngineBuilder().UseEmbeddedResourcesProject(typeof(Root));

			var engine = GetEngine().AddDefaultNamespaces("123")
				.UseOptions(new RazorLightOptions{Namespaces = new HashSet<string>{"123"}});
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().AddDynamicTemplates(new Dictionary<string, string>{ {"abc", "123"} })
				.UseOptions(new RazorLightOptions {DynamicTemplates = new Dictionary<string, string> { { "abc", "123" } } });
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().AddMetadataReferences(MetadataReference.CreateFromStream(new MemoryStream()))
				.UseOptions(new RazorLightOptions {AdditionalMetadataReferences = new HashSet<MetadataReference>{ MetadataReference.CreateFromStream(new MemoryStream()) } });
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().ExcludeAssemblies("123")
                .UseOptions(new RazorLightOptions{ ExcludedAssemblies = new HashSet<string>{ "123"}});
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().AddPrerenderCallbacks(x => x.Layout = "123")
				.UseOptions(new RazorLightOptions {PreRenderCallbacks = new List<Action<ITemplatePage>> {x => x.Layout = "123"}});
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().UseMemoryCachingProvider()
				.UseOptions(new RazorLightOptions{CachingProvider = new MemoryCachingProvider()});
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().UseOutputEncoder(new TemplatePageTest.TestOutputEncoder())
				.UseOptions(new RazorLightOptions { OutputEncoder = new TemplatePageTest.TestOutputEncoder() });
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().EnableDebugMode()
				.UseOptions(new RazorLightOptions { EnableDebugMode = false });
			Assert.Throws<RazorLightException>(() => engine.Build());

			engine = GetEngine().EnableDebugMode(false)
				.UseOptions(new RazorLightOptions { EnableDebugMode = true });
			Assert.Throws<RazorLightException>(() => engine.Build());
		}

		[Fact]
		public void EngineBuilder_Can_Set_OutputEncoder_Only_Once()
		{
			var encoder = new TemplatePageTest.TestOutputEncoder();
			var engine = new RazorLightEngineBuilder().UseOutputEncoder(encoder);
			Assert.NotNull(engine);

			Assert.Throws<RazorLightException>(() => new RazorLightEngineBuilder()
				.UseOutputEncoder(encoder)
				.UseOutputEncoder(encoder));
		}

		[Fact]
		public void OutputEncoder_Defaults_To_PlainText()
		{
			var engine = new RazorLightEngineBuilder()
				.UseMemoryCachingProvider()
				.UseEmbeddedResourcesProject(typeof(Root))
				.Build();
			
			Assert.Same(PlainTextEncoder.Default, engine.Options.OutputEncoder);
		}

		[Fact]
		public void EnableDebug_Setting_Is_Set_Correctly()
		{
			Func<RazorLightEngineBuilder> GetEngine = () => new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root));

			// Default
			var engine = GetEngine()
				.Build();
			Assert.False(engine.Options.EnableDebugMode);

			// Set with EnableDebugMode
			engine = GetEngine()
				.EnableDebugMode()
				.Build();
			Assert.True(engine.Options.EnableDebugMode);

			engine = GetEngine()
				.EnableDebugMode(true)
				.Build();
			Assert.True(engine.Options.EnableDebugMode);

			engine = GetEngine()
				.EnableDebugMode(false)
				.Build();
			Assert.False(engine.Options.EnableDebugMode);

			// Set with UseOptions
			engine = GetEngine()
				.UseOptions(new RazorLightOptions { EnableDebugMode = true })
				.Build();
			Assert.True(engine.Options.EnableDebugMode);

			engine = GetEngine()
				.UseOptions(new RazorLightOptions { EnableDebugMode = false })
				.Build();
			Assert.False(engine.Options.EnableDebugMode);

			engine = GetEngine()
				.UseOptions(new RazorLightOptions())
				.Build();
			Assert.False(engine.Options.EnableDebugMode);
		}

		[Fact]
		public void MetadataReferencePolicy_Is_Set_Correctly()
		{
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root))
				.IncludeAssemblies("Application.TemplateContracts")
				.ExcludeAssemblies("Application.Internal.Data")
				.UseAllDependencyContextMetadataReferences()
				.Build();

			Assert.Contains("Application.TemplateContracts", engine.Options.IncludedAssemblies);
			Assert.Contains("Application.Internal.Data", engine.Options.ExcludedAssemblies);
			Assert.Equal(MetadataReferenceDiscoveryMode.All, engine.Options.MetadataReferenceDiscovery);
		}

		[Fact]
		public void Namespace_Setting_Is_Set_Correctly()
		{
			Func<RazorLightEngineBuilder> GetEngine = () => new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(typeof(Root));

			var namespaces = new [] { "abc", "def" };

			// Set namespaces with AddDefaultNamespaces
			var engine = GetEngine()
				.AddDefaultNamespaces(namespaces.ToArray())
				.Build();

			Assert.Equal(namespaces, engine.Options.Namespaces);

			// Set namespaces with UseOptions
			engine = GetEngine()
				.UseOptions(new RazorLightOptions { Namespaces = namespaces.ToHashSet()})
				.Build();

			Assert.Equal(namespaces, engine.Options.Namespaces);
		}

		//[Fact]
		//public void Compiler_OperatingAssembly_IsSetTo_EntryAssembly_If_Not_Specified()
		//{
		//    var engine = new RazorLightEngineBuilder().Build();

		//    Assert.Equal(engine.TemplateFactoryProvider.Compiler.OperatingAssembly, Assembly.GetEntryAssembly());
		//}
	}
}
