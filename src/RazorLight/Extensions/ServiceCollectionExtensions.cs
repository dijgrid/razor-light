using System;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorLight.Compatibility;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.DependencyInjection;
using RazorLight.Generation;
using RazorLight.Razor;

namespace RazorLight.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddRazorLight(this IServiceCollection services, Func<IRazorLightEngine> engineFactoryProvider)
		{
			if (services == null)
			{
				throw new ArgumentNullException(nameof(services));
			}

			if (engineFactoryProvider == null)
			{
				throw new ArgumentNullException(nameof(engineFactoryProvider));
			}

			services.TryAddSingleton<IRazorLightEngine>(p =>
			{
				var engine = engineFactoryProvider();
				ConfigureEngineServices(engine, p, new PropertyInjector());

				return engine;
			});

			return services;
		}

		public static RazorLightDependencyBuilder AddRazorLight(this IServiceCollection services)
		{
			services = services ?? throw new ArgumentNullException(nameof(services));
			services.AddOptions().Configure<RazorLightOptions>(options =>
			{
				options.OperatingAssembly = options.OperatingAssembly ?? Assembly.GetEntryAssembly();
			});
			services.TryAddSingleton<ICachingProvider, MemoryCachingProvider>();
			services.TryAddSingleton<RazorLightProject, NoRazorProject>();
			services.TryAddSingleton<IRazorLightEngine>(CreateEngine);

			RazorLightDependencyBuilder builder = new RazorLightDependencyBuilder(services);

			return builder;
		}

		private static IRazorLightEngine CreateEngine(IServiceProvider provider)
		{
			var options = RazorLightOptionsSnapshot.Create(
				provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RazorLightOptions>>().Value).Options;
			var project = provider.GetRequiredService<RazorLightProject>();
			var cache = provider.GetService<ICachingProvider>();
			var sourceGenerator = new RazorSourceGenerator(
				Razor6CompilerCompatibility.CreateEngine(),
				project,
				options.Namespaces,
				options.EnableDebugMode ?? false,
				options);
			var metadataReferences = new DefaultMetadataReferenceManager(
				options.AdditionalMetadataReferences,
				options.IncludedAssemblies,
				options.ExcludedAssemblies,
				options.MetadataReferenceDiscovery);
			var compilationService = new RoslynCompilationService(
				metadataReferences,
				options.OperatingAssembly ?? throw new InvalidOperationException("RazorLightOptions.OperatingAssembly must be configured."),
				options.EnableDebugMode ?? false,
				cache as IPrecompileCallback);
			var compiler = new RazorTemplateCompiler(sourceGenerator, compilationService, project, options);
			var handler = new EngineHandler(options, compiler, new TemplateFactoryProvider(), cache);
			var propertyInjector = new PropertyInjector();
			handler.ConfigureServices(provider.GetRequiredService<IServiceScopeFactory>(), propertyInjector);

			return new RazorLightEngine(handler);
		}

		private static void ConfigureEngineServices(
			IRazorLightEngine engine,
			IServiceProvider services,
			PropertyInjector propertyInjector)
		{
			if (engine is RazorLightEngine razorLightEngine)
			{
				razorLightEngine.ConfigureServices(
					services.GetRequiredService<IServiceScopeFactory>(),
					propertyInjector);
			}
		}
	}
}
