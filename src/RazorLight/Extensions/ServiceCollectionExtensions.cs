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

			services.AddSingleton<PropertyInjector>();
			services.TryAddSingleton<IEngineHandler>(p =>
				throw new InvalidOperationException($"This exception can only occur if you inject {nameof(IEngineHandler)} directly using {nameof(ServiceCollectionExtensions)}.{nameof(AddRazorLight)}"));
			services.TryAddSingleton<IRazorLightEngine>(p =>
			{
				var engine = engineFactoryProvider();
				ConfigureEngineServices(engine, p);

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
			services.TryAddSingleton<PropertyInjector>();
			services.TryAddSingleton<ICachingProvider, MemoryCachingProvider>();
			services.TryAddSingleton<RazorLightProject, NoRazorProject>();
			services.TryAddSingleton(provider => RazorLightOptionsSnapshot.Create(
				provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RazorLightOptions>>().Value));
			services.TryAddSingleton<RazorEngine>(Razor6CompilerCompatibility.CreateEngine());
			services.TryAddSingleton<RazorSourceGenerator>(provider =>
			{
				var options = provider.GetRequiredService<RazorLightOptionsSnapshot>().Options;
				return new RazorSourceGenerator(
					provider.GetRequiredService<RazorEngine>(),
					provider.GetRequiredService<RazorLightProject>(),
					options.Namespaces,
					options.EnableDebugMode ?? false,
					options);
			});
			services.TryAddSingleton<ITemplateFactoryProvider, TemplateFactoryProvider>();
			services.TryAddSingleton<IMetadataReferenceManager>(provider =>
			{
				var options = provider.GetRequiredService<RazorLightOptionsSnapshot>().Options;
				return new DefaultMetadataReferenceManager(
					options.AdditionalMetadataReferences,
					options.IncludedAssemblies,
					options.ExcludedAssemblies,
					options.MetadataReferenceDiscovery);
			});
			services.TryAddSingleton<ICompilationService>(provider =>
			{
				var options = provider.GetRequiredService<RazorLightOptionsSnapshot>().Options;
				return new RoslynCompilationService(
					provider.GetRequiredService<IMetadataReferenceManager>(),
					options.OperatingAssembly ?? throw new InvalidOperationException("RazorLightOptions.OperatingAssembly must be configured."),
					options.EnableDebugMode ?? false,
					provider.GetService<IPrecompileCallback>());
			});
			services.TryAddSingleton<IRazorTemplateCompiler>(provider => new RazorTemplateCompiler(
				provider.GetRequiredService<RazorSourceGenerator>(),
				provider.GetRequiredService<ICompilationService>(),
				provider.GetRequiredService<RazorLightProject>(),
				provider.GetRequiredService<RazorLightOptionsSnapshot>().Options));
			services.TryAddSingleton<IEngineHandler>(provider =>
			{
				var handler = new EngineHandler(
					provider.GetRequiredService<RazorLightOptionsSnapshot>().Options,
					provider.GetRequiredService<IRazorTemplateCompiler>(),
					provider.GetRequiredService<ITemplateFactoryProvider>(),
					provider.GetService<ICachingProvider>());
				handler.ConfigureServices(
					provider.GetRequiredService<IServiceScopeFactory>(),
					provider.GetRequiredService<PropertyInjector>());
				return handler;
			});
			services.TryAddSingleton<IRazorLightEngine, RazorLightEngine>();

			RazorLightDependencyBuilder builder = new RazorLightDependencyBuilder(services);

			return builder;
		}

		private static void ConfigureEngineServices(IRazorLightEngine engine, IServiceProvider services)
		{
			if (engine is RazorLightEngine razorLightEngine)
			{
				razorLightEngine.ConfigureServices(
					services.GetRequiredService<IServiceScopeFactory>(),
					services.GetRequiredService<PropertyInjector>());
			}
		}
	}
}
