using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Caching;
using RazorLight.Compatibility;
using RazorLight.Compilation;
using RazorLight.DependencyInjection;
using RazorLight.Generation;
using RazorLight.Razor;

namespace RazorLight
{
	internal static class RazorLightEngineFactory
	{
		public static RazorLightEngine CreatePrecompiled(
			RazorLightOptions options,
			ICachingProvider cache,
			IDisposable? ownedCache = null,
			IServiceScopeFactory? scopeFactory = null)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			options.CachingProvider = cache;
			var handler = new EngineHandler(
				options,
				new PrecompiledTemplateCompiler(),
				new UnavailableTemplateFactoryProvider(),
				cache,
				ownedCachingProvider: ownedCache);
			if (scopeFactory != null)
			{
				handler.ConfigureServices(scopeFactory, new PropertyInjector());
			}

			return new RazorLightEngine(handler);
		}

		public static RazorLightEngine Create(
			RazorLightOptions options,
			RazorLightProject project,
			ICachingProvider? cache,
			Assembly operatingAssembly,
			IDisposable? ownedProject = null,
			IDisposable? ownedCache = null,
			IServiceScopeFactory? scopeFactory = null)
		{
			if (options == null) throw new ArgumentNullException(nameof(options));
			if (project == null) throw new ArgumentNullException(nameof(project));
			if (operatingAssembly == null) throw new ArgumentNullException(nameof(operatingAssembly));

			options.CachingProvider = cache;
			options.OperatingAssembly = operatingAssembly;
			var references = new DefaultMetadataReferenceManager(
				options.AdditionalMetadataReferences,
				options.IncludedAssemblies,
				options.ExcludedAssemblies,
				options.MetadataReferenceDiscovery);
			var compilation = new RoslynCompilationService(
				references,
				operatingAssembly,
				options.EnableDebugMode ?? false,
				options.RedactCompilerDiagnosticMessages,
				cache as IPrecompileCallback);
			var sourceGenerator = new RazorSourceGenerator(
				Razor6CompilerCompatibility.CreateEngine(),
				project,
				options.Namespaces,
				options.EnableDebugMode ?? false,
				options);
			var compiler = new RazorTemplateCompiler(sourceGenerator, compilation, project, options);
			var handler = new EngineHandler(
				options,
				compiler,
				new TemplateFactoryProvider(),
				cache,
				ownedProject,
				ownedCache);

			if (scopeFactory != null)
			{
				handler.ConfigureServices(scopeFactory, new PropertyInjector());
			}

			return new RazorLightEngine(handler);
		}
	}
}
