using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Razor;
using RazorLight.Text;

namespace RazorLight
{
	/// <summary>
	/// Builds an immutable RazorLight engine configuration. Use <see cref="RazorLightProject"/>,
	/// <see cref="ICachingProvider"/>, or <see cref="IOutputEncoder"/> for supported customization.
	/// </summary>
	public sealed class RazorLightEngineBuilder
	{
		/// <summary>
		/// Creates an engine that can render only page factories already present in
		/// <paramref name="provider"/>. This entry point has no runtime compiler construction path.
		/// </summary>
		public static IRazorLightEngine CreatePrecompiled(
			ICachingProvider provider,
			RazorLightOptions? options = null)
		{
			if (provider == null) throw new ArgumentNullException(nameof(provider));
			var snapshot = RazorLightOptionsSnapshot.CreatePrecompiled(options ?? new RazorLightOptions()).Options;
			if (snapshot.CachingProvider != null && !ReferenceEquals(snapshot.CachingProvider, provider))
			{
				throw new RazorLightException("The options caching provider conflicts with the precompiled provider argument.");
			}

			return RazorLightEngineFactory.CreatePrecompiled(snapshot, provider);
		}

		private Assembly? operatingAssembly;

		private HashSet<string>? namespaces;

		private ConcurrentDictionary<string, string>? dynamicTemplates;

		private HashSet<string>? csharpSourceKeys;

		private ConcurrentDictionary<string, string>? dynamicCSharpSources;

		private HashSet<MetadataReference>? metadataReferences;

		private HashSet<string>? includedAssemblies;

		private HashSet<string>? excludedAssemblies;

		private MetadataReferenceDiscoveryMode? metadataReferenceDiscovery;

		private readonly List<Action<ITemplatePage>> pageInitializers = new List<Action<ITemplatePage>>();

		private RazorLightProject? project;

		private ICachingProvider? cachingProvider;
		private bool ownsProject;
		private bool ownsCachingProvider;
		private bool precompiledOnly;

		private IOutputEncoder? outputEncoder;

		private bool? enableDebugMode;

		private RazorLightOptions? options;


		/// <summary>
		/// Configures RazorLight to use a project.
		/// </summary>
		/// <remarks>
		/// Use this if implementing a custom <see cref="RazorLightProject"/>.
		/// </remarks>
		/// <param name="razorLightProject"></param>
		/// <returns></returns>
		public RazorLightEngineBuilder UseProject(RazorLightProject razorLightProject)
		{
			project = razorLightProject ?? throw new ArgumentNullException(nameof(razorLightProject), $"Use {nameof(NoRazorProject)} instead of null.  See also {nameof(UseNoProject)}.");
			ownsProject = false;

			return this;
		}

		/// <summary>
		/// Configures RazorLight to use a project whose persistent store is a "null device".
		/// </summary>
		public RazorLightEngineBuilder UseNoProject()
		{
			project = new NoRazorProject();
			ownsProject = true;

			return this;
		}

		/// <summary>
		/// Configures RazorLight to use a project whose persistent store is the file system.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		public RazorLightEngineBuilder UseFileSystemProject(string root)
		{
			project = new FileSystemRazorProject(root);
			ownsProject = true;

			return this;
		}

		/// <summary>
		/// Configures RazorLight to use a project whose persistent store is the file system.
		/// </summary>
		/// <param name="root">Directory path to the root folder containing your Razor markup files.</param>
		/// <param name="extension">If you wish, you can use a different extension than .cshtml.</param>
		/// <returns><see cref="RazorLightEngineBuilder"/></returns>
		public RazorLightEngineBuilder UseFileSystemProject(string root, string extension)
		{
			project = new FileSystemRazorProject(root, extension);
			ownsProject = true;

			return this;
		}

		/// <summary>
		/// Configures RazorLight to use a project whose persistent store an assembly manifest resource stream.
		/// </summary>
		/// <param name="rootType">Any type in the root namespace (prefix) for your assembly manifest resource stream.</param>
		/// <returns><see cref="EmbeddedRazorProject"/></returns>
		public RazorLightEngineBuilder UseEmbeddedResourcesProject(Type rootType)
		{
			if (rootType == null) throw new ArgumentNullException(nameof(rootType));

			project = new EmbeddedRazorProject(rootType);
			ownsProject = true;

			return this;
		}

		/// <summary>
		/// Configures RazorLight to use a project whose persistent store an assembly manifest resource stream.
		/// </summary>
		/// <param name="assembly">Assembly containing embedded resources</param>
		/// <param name="rootNamespace">The root namespace (prefix) for your assembly manifest resource stream.</param>
		/// <returns></returns>
		public RazorLightEngineBuilder UseEmbeddedResourcesProject(Assembly assembly, string? rootNamespace = null)
		{
			project = new EmbeddedRazorProject(assembly, rootNamespace);
			ownsProject = true;

			return this;
		}

		/// <summary>Uses a complete options object as the starting configuration snapshot.</summary>
		public RazorLightEngineBuilder UseOptions(RazorLightOptions razorLightOptions)
		{
			options = razorLightOptions ?? throw new ArgumentNullException(nameof(razorLightOptions));
			return this;
		}

		/// <summary>
		/// Configures a transformation for expression values. Template literals and
		/// <see cref="ITemplateContent"/> values bypass this encoder.
		/// </summary>
		public RazorLightEngineBuilder UseOutputEncoder(IOutputEncoder encoder)
		{
			if (encoder == null) throw new ArgumentNullException(nameof(encoder));
			if (outputEncoder != null)
				throw new RazorLightException($"{nameof(outputEncoder)} has already been set");

			outputEncoder = encoder;
			return this;
		}

		/// <summary>Enables the built-in process-local compiled page cache, owned by the engine.</summary>
		public RazorLightEngineBuilder UseMemoryCachingProvider()
		{
			cachingProvider = new MemoryCachingProvider();
			ownsCachingProvider = true;

			return this;
		}

		/// <summary>Uses a caller-owned custom compiled page cache.</summary>
		public RazorLightEngineBuilder UseCachingProvider(ICachingProvider provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException(nameof(provider));
			}

			cachingProvider = provider;
			ownsCachingProvider = false;

			return this;
		}

		/// <summary>
		/// Uses only page factories supplied by <paramref name="provider"/> and disables all runtime
		/// Razor generation and Roslyn compilation.
		/// </summary>
		public RazorLightEngineBuilder UsePrecompiledOnly(ICachingProvider provider)
		{
			UseCachingProvider(provider);
			precompiledOnly = true;
			return this;
		}

		/// <summary>Adds namespace imports to every generated template.</summary>
		public RazorLightEngineBuilder AddDefaultNamespaces(params string[] namespaces)
		{
			if (namespaces == null)
			{
				throw new ArgumentNullException(nameof(namespaces));
			}

			this.namespaces ??= new HashSet<string>();
			foreach (string @namespace in namespaces)
				this.namespaces.Add(@namespace ?? throw new ArgumentException("Namespace values cannot be null.", nameof(namespaces)));

			return this;
		}

		/// <summary>Adds explicit Roslyn metadata references for template compilation.</summary>
		public RazorLightEngineBuilder AddMetadataReferences(params MetadataReference[] metadata)
		{
			if (metadata == null)
			{
				throw new ArgumentNullException(nameof(metadata));
			}

			metadataReferences ??= new HashSet<MetadataReference>();
			foreach (var reference in metadata)
				metadataReferences.Add(reference ?? throw new ArgumentException("Metadata references cannot contain null.", nameof(metadata)));

			return this;
		}

		/// <summary>Excludes exact assembly names from automatic metadata-reference discovery.</summary>
		public RazorLightEngineBuilder ExcludeAssemblies(params string[] assemblyNames)
		{
			if (assemblyNames == null)
			{
				throw new ArgumentNullException(nameof(assemblyNames));
			}

			excludedAssemblies ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var assemblyName in assemblyNames)
				excludedAssemblies.Add(assemblyName ?? throw new ArgumentException("Assembly names cannot contain null.", nameof(assemblyNames)));

			return this;
		}

		/// <summary>
		/// Adds named assemblies from the operating assembly's dependency context to minimal metadata
		/// reference discovery. Assembly names are matched exactly and without regard to case.
		/// </summary>
		public RazorLightEngineBuilder IncludeAssemblies(params string[] assemblyNames)
		{
			if (assemblyNames == null)
			{
				throw new ArgumentNullException(nameof(assemblyNames));
			}

			includedAssemblies ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string assemblyName in assemblyNames)
				includedAssemblies.Add(assemblyName ?? throw new ArgumentException("Assembly names cannot contain null.", nameof(assemblyNames)));
			return this;
		}

		/// <summary>
		/// Enables compatibility discovery of every compile-time library in the operating assembly's
		/// dependency context. Templates can compile against all host dependencies in this mode.
		/// </summary>
		public RazorLightEngineBuilder UseAllDependencyContextMetadataReferences()
		{
			metadataReferenceDiscovery = MetadataReferenceDiscoveryMode.All;
			return this;
		}

		/// <summary>
		/// Adds an initializer that runs once for every rendered page, including layouts and includes.
		/// </summary>
		public RazorLightEngineBuilder AddPageInitializer(Action<ITemplatePage> initializer)
		{
			if (initializer == null)
			{
				throw new ArgumentNullException(nameof(initializer));
			}

			pageInitializers.Add(initializer);

			return this;
		}

		/// <summary>Seeds keyed in-memory templates that can be rendered through project-style APIs.</summary>
		public RazorLightEngineBuilder AddDynamicTemplates(IDictionary<string, string> dynamicTemplates)
		{
			if (dynamicTemplates == null)
			{
				throw new ArgumentNullException(nameof(dynamicTemplates));
			}

			this.dynamicTemplates = new ConcurrentDictionary<string, string>(dynamicTemplates);

			return this;
		}

		/// <summary>
		/// Compiles a project C# source with every template created by this engine.
		/// </summary>
		public RazorLightEngineBuilder AddCSharpSource(string sourceKey)
		{
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			}

			csharpSourceKeys ??= new HashSet<string>(StringComparer.Ordinal);
			csharpSourceKeys.Add(sourceKey);
			return this;
		}

		/// <summary>
		/// Registers in-memory C# source and compiles it with every template created by this engine.
		/// </summary>
		public RazorLightEngineBuilder AddCSharpSource(string sourceKey, string sourceContent)
		{
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			}
			if (sourceContent == null)
			{
				throw new ArgumentNullException(nameof(sourceContent));
			}

			dynamicCSharpSources ??= new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			dynamicCSharpSources[sourceKey] = sourceContent;
			return AddCSharpSource(sourceKey);
		}

		/// <summary>Sets the assembly whose dependency context supplies compilation references.</summary>
		public RazorLightEngineBuilder SetOperatingAssembly(Assembly assembly)
		{
			if (assembly == null)
			{
				throw new ArgumentNullException(nameof(assembly));
			}

			operatingAssembly = assembly;

			return this;
		}

		/// <summary>Controls detailed template lookup and source diagnostics.</summary>
		public RazorLightEngineBuilder EnableDebugMode(bool enableDebugMode = true)
		{
			this.enableDebugMode = enableDebugMode;
			return this;
		}

		/// <summary>Snapshots the configuration and creates an engine that owns builder-created resources.</summary>
		public IRazorLightEngine Build()
		{
			var buildOptions = RazorLightOptionsSnapshot.Create(options ?? new RazorLightOptions()).Options;
			project = project ?? new NoRazorProject();

			if (namespaces != null)
			{
				if (namespaces.Count > 0 && buildOptions.Namespaces.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(namespaces));

				buildOptions.Namespaces = new HashSet<string>(namespaces);
			}

			if (dynamicTemplates != null)
			{
				if (dynamicTemplates.Count > 0 && buildOptions.DynamicTemplates.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(dynamicTemplates));

				buildOptions.DynamicTemplates = new ConcurrentDictionary<string, string>(dynamicTemplates);
			}

			if (csharpSourceKeys != null)
			{
				if (csharpSourceKeys.Count > 0 && buildOptions.CSharpSourceKeys.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(csharpSourceKeys));

				buildOptions.CSharpSourceKeys = new HashSet<string>(csharpSourceKeys, StringComparer.Ordinal);
			}

			if (dynamicCSharpSources != null)
			{
				if (dynamicCSharpSources.Count > 0 && buildOptions.DynamicCSharpSources.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(dynamicCSharpSources));

				buildOptions.DynamicCSharpSources = new ConcurrentDictionary<string, string>(dynamicCSharpSources, StringComparer.Ordinal);
			}

			if (metadataReferences != null)
			{
				if (metadataReferences.Count > 0 && buildOptions.AdditionalMetadataReferences.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(metadataReferences));

				buildOptions.AdditionalMetadataReferences = new HashSet<MetadataReference>(metadataReferences);
			}

			if (excludedAssemblies != null)
			{
				if (excludedAssemblies.Count > 0 && buildOptions.ExcludedAssemblies.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(excludedAssemblies));

				buildOptions.ExcludedAssemblies = new HashSet<string>(excludedAssemblies);
			}

			if (includedAssemblies != null)
			{
				if (includedAssemblies.Count > 0 && buildOptions.IncludedAssemblies.Count > 0)
					ThrowIfHasBeenSetExplicitly(nameof(includedAssemblies));

				buildOptions.IncludedAssemblies = new HashSet<string>(includedAssemblies, StringComparer.OrdinalIgnoreCase);
			}

			if (metadataReferenceDiscovery.HasValue)
			{
				if (buildOptions.MetadataReferenceDiscovery != MetadataReferenceDiscoveryMode.Minimal)
					ThrowIfHasBeenSetExplicitly(nameof(metadataReferenceDiscovery));

				buildOptions.MetadataReferenceDiscovery = metadataReferenceDiscovery.Value;
			}

			if (pageInitializers.Count > 0)
			{
				buildOptions.PageInitializers = new List<Action<ITemplatePage>>(pageInitializers);
			}

			if (cachingProvider != null)
			{
				if (buildOptions.CachingProvider != null)
					ThrowIfHasBeenSetExplicitly(nameof(cachingProvider));

				buildOptions.CachingProvider = cachingProvider;
			}

			if (outputEncoder != null)
			{
				if (!ReferenceEquals(buildOptions.OutputEncoder, PlainTextEncoder.Default))
					ThrowIfHasBeenSetExplicitly(nameof(outputEncoder));

				buildOptions.OutputEncoder = outputEncoder;
			}

			if (enableDebugMode.HasValue && buildOptions.EnableDebugMode.HasValue)
			{
				ThrowIfHasBeenSetExplicitly(nameof(enableDebugMode));
			}
			else
			{
				buildOptions.EnableDebugMode = buildOptions.EnableDebugMode ?? enableDebugMode ?? false;
			}

			if (precompiledOnly)
			{
				if (project != null && project is not NoRazorProject)
				{
					throw new RazorLightException("A source project cannot be combined with precompiled-only mode.");
				}

				return RazorLightEngineFactory.CreatePrecompiled(
					buildOptions,
					buildOptions.CachingProvider
						?? throw new RazorLightException("Precompiled-only mode requires a caching provider."),
					ownedCache: ownsCachingProvider ? buildOptions.CachingProvider as IDisposable : null);
			}
			var assembly = operatingAssembly ?? Assembly.GetEntryAssembly()
				?? throw new InvalidOperationException("An operating assembly could not be determined. Configure one with SetOperatingAssembly.");
			return RazorLightEngineFactory.Create(
				buildOptions,
				project,
				buildOptions.CachingProvider,
				assembly,
				ownedProject: ownsProject ? project as IDisposable : null,
				ownedCache: ownsCachingProvider ? buildOptions.CachingProvider as IDisposable : null);
		}

		private void ThrowIfHasBeenSetExplicitly(string option)
		{
			throw new RazorLightException($"{option} has conflicting settings, configure using either fluent configuration or setting an Options object.");
		}
	}
}
