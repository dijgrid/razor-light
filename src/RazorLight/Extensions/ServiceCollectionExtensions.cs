using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorLight.Caching;
using RazorLight.DependencyInjection;
using RazorLight.Razor;

namespace RazorLight.Extensions
{
	/// <summary>Registers RazorLight engines with Microsoft.Extensions.DependencyInjection.</summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>Registers a singleton engine created by the supplied factory.</summary>
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

		/// <summary>Begins dependency-injection configuration for a singleton RazorLight engine.</summary>
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
			return RazorLightEngineFactory.Create(
				options,
				project,
				cache,
				options.OperatingAssembly ?? throw new InvalidOperationException("RazorLightOptions.OperatingAssembly must be configured."),
				scopeFactory: provider.GetRequiredService<IServiceScopeFactory>());
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
