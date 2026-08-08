using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using RazorLight.Extensions;
using System;
using Microsoft.Extensions.Hosting;
using RazorLight.Compilation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System.Dynamic;
using System.IO;
using System.Linq;
using RazorLight.Razor;
using RazorLight.Tests.Utils;
using Microsoft.Extensions.Options;

namespace RazorLight.Tests.Extensions
{
	public class ServiceCollectionExtensionsTest
	{
		private readonly string _rootPath = DirectoryUtils.RootDirectory;

		private IServiceCollection GetServices()
		{
			return new ServiceCollection();
		}

		[Fact]
		public void Throws_On_Null_EngineFactoryProvider()
		{
			var services = GetServices();

			Assert.Throws<ArgumentNullException>(() => { services.AddRazorLight(null!); });
		}

		[Fact]
		public void Dependency_Builder_Add_Methods_Accumulate_Values()
		{
			var firstReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
			var secondReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
			var services = GetServices();
			services.AddRazorLight()
				.AddDefaultNamespaces("One")
				.AddDefaultNamespaces("Two")
				.AddMetadataReferences(firstReference)
				.AddMetadataReferences(secondReference)
				.IncludeAssemblies("First")
				.IncludeAssemblies("Second")
				.ExcludeAssemblies("Third")
				.ExcludeAssemblies("Fourth");

			using var provider = services.BuildServiceProvider();
			var options = provider.GetRequiredService<IOptions<RazorLightOptions>>().Value;
			Assert.Equal(new[] { "One", "Two" }, options.Namespaces.OrderBy(value => value));
			Assert.Equal(2, options.AdditionalMetadataReferences.Count);
			Assert.Equal(new[] { "First", "Second" }, options.IncludedAssemblies.OrderBy(value => value));
			Assert.Equal(new[] { "Fourth", "Third" }, options.ExcludedAssemblies.OrderBy(value => value));
		}

		[Fact]
		public void Ensure_FactoryMethod_Is_Called()
		{
			var services = GetServices();
			bool called = false;

			services.AddRazorLight(() =>
			{
				called = true;
				return new RazorLightEngineBuilder()
					.UseEmbeddedResourcesProject(typeof(Root).Assembly).Build();
			});

			var provider = services.BuildServiceProvider();
			var engine = provider.GetService<IRazorLightEngine>();

			Assert.NotNull(engine);
			Assert.IsType<RazorLightEngine>(engine);
			Assert.True(called);
		}

		public class EmbeddedEngineStartup
		{
			public void ConfigureServices(IServiceCollection services)
			{
				var embeddedEngine = new RazorLightEngineBuilder()
					.UseEmbeddedResourcesProject(typeof(EmbeddedEngineStartup)) // exception without this (or another project type)
					.UseMemoryCachingProvider()
					.Build();

				services.AddRazorLight(() => embeddedEngine);
			}
		}

		[Fact]
		public void Ensure_Works_With_Generic_Host()
		{
			static IHostBuilder CreateHostBuilder(string[]? args)
			{
				return Host.CreateDefaultBuilder(args)
					.ConfigureServices(services => new EmbeddedEngineStartup().ConfigureServices(services));
			}

			var hostBuilder = CreateHostBuilder(null);

			Assert.NotNull(hostBuilder);
			var host = hostBuilder.Build();
			Assert.NotNull(host);
			host.Services.GetService<IRazorLightEngine>();
		}

		[Fact]
		public void Registers_Only_Supported_RazorLight_Service_Contracts()
		{
			var services = GetServices();
			services.AddRazorLight();

			Assert.DoesNotContain(services, descriptor =>
				descriptor.ServiceType.Assembly == typeof(IRazorLightEngine).Assembly &&
				!descriptor.ServiceType.IsPublic &&
				!descriptor.ServiceType.IsNestedPublic);
		}

		[Fact()]
		public void Ensure_Works_With_Generic_Host_and_DefaultServiceProvider()
		{
			static IHostBuilder CreateHostBuilder(string[]? args)
			{
				return Host.CreateDefaultBuilder(args)
					.UseDefaultServiceProvider((context, options) =>
					{
						options.ValidateScopes = false;
						options.ValidateOnBuild = false;
					})
					.ConfigureServices(services => new EmbeddedEngineStartup().ConfigureServices(services));
			}

			var hostBuilder = CreateHostBuilder(null);

			Assert.NotNull(hostBuilder);
			var host = hostBuilder.Build();
			Assert.NotNull(host);
			host.Services.GetService<IRazorLightEngine>();
		}

		[Fact()]
		public void Ensure_Works_With_Generic_Host_and_DefaultServiceProvider_ValidateScopes_ValidateOnBuild()
		{
			static IHostBuilder CreateHostBuilder(string[]? args)
			{
				return Host.CreateDefaultBuilder(args)
					.UseDefaultServiceProvider((context, options) =>
					{
						options.ValidateScopes = true;
						options.ValidateOnBuild = true;
					})
					.ConfigureServices(services => new EmbeddedEngineStartup().ConfigureServices(services));
			}

			var hostBuilder = CreateHostBuilder(null);

			Assert.NotNull(hostBuilder);
			var host = hostBuilder.Build();
			Assert.NotNull(host);
			host.Services.GetService<IRazorLightEngine>();
		}

		[Fact()]
		public void Ensure_Works_With_Generic_Host_and_DefaultServiceProvider_ValidateOnBuild()
		{
			static IHostBuilder CreateHostBuilder(string[]? args)
			{
				return Host.CreateDefaultBuilder(args)
					.UseDefaultServiceProvider((context, options) =>
					{
						options.ValidateScopes = false;
						options.ValidateOnBuild = true;
					})
					.ConfigureServices(services => new EmbeddedEngineStartup().ConfigureServices(services));
			}

			var hostBuilder = CreateHostBuilder(null);

			Assert.NotNull(hostBuilder);
			var host = hostBuilder.Build();
			Assert.NotNull(host);
			host.Services.GetService<IRazorLightEngine>();
		}

		[Fact()]
		public void Ensure_Works_With_Generic_Host_and_DefaultServiceProvider_ValidateScopes()
		{
			static IHostBuilder CreateHostBuilder(string[]? args)
			{
				return Host.CreateDefaultBuilder(args)
					.UseDefaultServiceProvider((context, options) =>
					{
						options.ValidateScopes = true;
						options.ValidateOnBuild = false;
					})
					.ConfigureServices(services => new EmbeddedEngineStartup().ConfigureServices(services));
			}

			var hostBuilder = CreateHostBuilder(null);

			Assert.NotNull(hostBuilder);
			var host = hostBuilder.Build();
			Assert.NotNull(host);
			host.Services.GetService<IRazorLightEngine>();
		}

		[Fact]
		public void Ensure_BuilderFactory_Is_Called()
		{
			var services = GetServices();
			var called = false;

			services.AddRazorLight(() =>
			{
				called = true;
				return new RazorLightEngineBuilder()
					.UseFileSystemProject(_rootPath)
					.UseMemoryCachingProvider()
					.Build();
			});

			var provider = services.BuildServiceProvider();
			var engine = provider.GetService<IRazorLightEngine>();

			Assert.NotNull(engine);
			Assert.IsType<RazorLightEngine>(engine);
			Assert.True(called);
		}

		[Fact]
		public async Task Ensure_DI_Extension_Can_Inject()
		{
			var services = GetServices();
			bool newRazorLightEngineCalled = false;

			services.AddRazorLight()
				.UseMemoryCachingProvider()
				.UseFileSystemProject(_rootPath);

			services.RemoveAll<IRazorLightEngine>();
			services.AddSingleton<IRazorLightEngine>(new TestRazorLightEngine(() =>
			{
				newRazorLightEngineCalled = true;
			}));

			var provider = services.BuildServiceProvider();
			var project = provider.GetService<RazorLightProject>();
			Assert.IsType<FileSystemRazorProject>(project);
			var fileSystemProject = (FileSystemRazorProject)project;
			Assert.Equal(_rootPath, fileSystemProject.Root);

			var engine = provider.GetService<IRazorLightEngine>();
			Assert.NotNull(engine);
			Assert.IsType<TestRazorLightEngine>(engine);
			await engine.CompileRenderStringAsync("", "", "");
			Assert.True(newRazorLightEngineCalled);

		}

		public class TestRazorLightEngine : IRazorLightEngine
		{

			private readonly Action _compileAction;
			public TestRazorLightEngine(Action compileAction)
			{
				_compileAction = compileAction;
			}

			public bool IsTemplateCached(string key) => false;

			public void InvalidateTemplate(string key)
			{
			}

			public Task<string> CompileRenderAsync<T>(string key, T model, ExpandoObject? viewBag = null)
			{
				throw new NotImplementedException();
			}

			public Task<string> CompileRenderAsync(string key, object? model, Type modelType, ExpandoObject? viewBag = null)
			{
				throw new NotImplementedException();
			}

			public Task<string> CompileRenderStringAsync<T>(string key, string content, T model, ExpandoObject? viewBag = null)
			{
				_compileAction();
				var result = nameof(TestRazorLightEngine);
				return Task.FromResult(result);
			}

			public Task<string> CompileRenderStringAsync(string key, string content, object? model, Type modelType, ExpandoObject? viewBag = null)
			{
				_compileAction();
				return Task.FromResult(nameof(TestRazorLightEngine));
			}

			public Task<ITemplatePage> CompileTemplateAsync(string key)
			{
				throw new NotImplementedException();
			}

			public Task<string> RenderTemplateAsync<T>(ITemplatePage templatePage, T model, ExpandoObject? viewBag = null)
			{
				throw new NotImplementedException();
			}

			public Task RenderTemplateAsync<T>(ITemplatePage templatePage, T model, TextWriter textWriter, ExpandoObject? viewBag = null)
			{
				throw new NotImplementedException();
			}
		}

		[Fact]
		public async Task Try_Render_With_DI_Extension()
		{
			var path = DirectoryUtils.RootDirectory;

			var services = GetServices();
			services.AddRazorLight()
				.UseMemoryCachingProvider()
				.UseFileSystemProject(Path.Combine(path, "Assets", "Files"));

			var provider = services.BuildServiceProvider();
			var engine = provider.GetRequiredService<IRazorLightEngine>();
			var result = await engine.CompileRenderAsync<object?>("template1.cshtml", null);
		}
	}
}
