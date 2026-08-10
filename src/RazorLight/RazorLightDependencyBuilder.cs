using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Razor;

namespace RazorLight
{
	/// <summary>Configures the singleton RazorLight engine registered with dependency injection.</summary>
	public sealed class RazorLightDependencyBuilder
	{
		private readonly IServiceCollection _services;
		/// <summary>Creates a builder over the supplied service collection.</summary>
		public RazorLightDependencyBuilder(IServiceCollection services)
		{
			_services = services;
		}

		/// <summary>Uses file-system template lookup rooted at the supplied contained directory.</summary>
		public RazorLightDependencyBuilder UseFileSystemProject(string root, string? extension = null)
		{
			_services.RemoveAll<RazorLightProject>();

			RazorLightProject project;
			if (string.IsNullOrEmpty(extension))
			{
				project = new FileSystemRazorProject(root);
			}
			else
			{
				project = new FileSystemRazorProject(root, extension);
			}

			// ReSharper disable once RedundantTypeArgumentsOfMethod
			_services.AddSingleton<RazorLightProject>(project);
			return this;
		}

		/// <summary>Enables the process-local memory cache for compiled pages.</summary>
		public RazorLightDependencyBuilder UseMemoryCachingProvider()
		{
			_services.RemoveAll<ICachingProvider>();
			_services.AddSingleton<ICachingProvider, MemoryCachingProvider>();
			return this;
		}

		/// <summary>Uses embedded Razor resources rooted at the namespace of <paramref name="rootType"/>.</summary>
		public RazorLightDependencyBuilder UseEmbeddedResourcesProject(Type rootType)
		{
			_services.RemoveAll<RazorLightProject>();
			RazorLightProject project = new EmbeddedRazorProject(rootType);
			// ReSharper disable once RedundantTypeArgumentsOfMethod
			_services.AddSingleton<RazorLightProject>(project);
			return this;
		}

		/// <summary>Sets the assembly whose dependency context supplies compilation references.</summary>
		public RazorLightDependencyBuilder SetOperatingAssembly(Assembly assembly)
		{
			_services.Configure<RazorLightOptions>(x => x.OperatingAssembly = assembly);
			return this;
		}

		/// <summary>Adds namespace imports to every generated template.</summary>
		public RazorLightDependencyBuilder AddDefaultNamespaces(params string[] namespaces)
		{
			if (namespaces == null) throw new ArgumentNullException(nameof(namespaces));
			if (Array.Exists(namespaces, value => value == null)) throw new ArgumentException("Namespace values cannot contain null.", nameof(namespaces));
			_services.Configure<RazorLightOptions>(options => options.Namespaces.UnionWith(namespaces));
			return this;
		}

		/// <summary>Excludes exact assembly names from automatic metadata-reference discovery.</summary>
		public RazorLightDependencyBuilder ExcludeAssemblies(params string[] assemblyNames)
		{
			if (assemblyNames == null) throw new ArgumentNullException(nameof(assemblyNames));
			if (Array.Exists(assemblyNames, value => value == null)) throw new ArgumentException("Assembly names cannot contain null.", nameof(assemblyNames));
			_services.Configure<RazorLightOptions>(x => x.ExcludedAssemblies.UnionWith(assemblyNames));
			return this;
		}

		/// <summary>Includes exact assembly names in minimal metadata-reference discovery.</summary>
		public RazorLightDependencyBuilder IncludeAssemblies(params string[] assemblyNames)
		{
			if (assemblyNames == null)
			{
				throw new ArgumentNullException(nameof(assemblyNames));
			}

			if (Array.Exists(assemblyNames, value => value == null)) throw new ArgumentException("Assembly names cannot contain null.", nameof(assemblyNames));
			_services.Configure<RazorLightOptions>(x => x.IncludedAssemblies.UnionWith(assemblyNames));
			return this;
		}

		/// <summary>Enables broad compatibility discovery of the operating assembly's dependencies.</summary>
		public RazorLightDependencyBuilder UseAllDependencyContextMetadataReferences()
		{
			_services.Configure<RazorLightOptions>(x =>
				x.MetadataReferenceDiscovery = MetadataReferenceDiscoveryMode.All);
			return this;
		}

		/// <summary>
		/// Adds an initializer that runs once for every rendered page, including layouts and includes.
		/// </summary>
		public RazorLightDependencyBuilder AddPageInitializer(Action<ITemplatePage> initializer)
		{
			if (initializer == null)
			{
				throw new ArgumentNullException(nameof(initializer));
			}

			_services.Configure<RazorLightOptions>(options => options.PageInitializers.Add(initializer));
			return this;
		}

		/// <summary>Adds explicit Roslyn references for template compilation.</summary>
		public RazorLightDependencyBuilder AddMetadataReferences(params MetadataReference[] metadata)
		{
			if (metadata == null) throw new ArgumentNullException(nameof(metadata));
			if (Array.Exists(metadata, value => value == null)) throw new ArgumentException("Metadata references cannot contain null.", nameof(metadata));
			_services.Configure<RazorLightOptions>(x => x.AdditionalMetadataReferences.UnionWith(metadata));
			return this;
		}

		/// <summary>Adds a project C# source key to every template compilation.</summary>
		public RazorLightDependencyBuilder AddCSharpSource(string sourceKey)
		{
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			}

			_services.Configure<RazorLightOptions>(options => options.CSharpSourceKeys.Add(sourceKey));
			return this;
		}

		/// <summary>Adds keyed in-memory C# source to every template compilation.</summary>
		public RazorLightDependencyBuilder AddCSharpSource(string sourceKey, string sourceContent)
		{
			if (string.IsNullOrWhiteSpace(sourceKey)) throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			if (sourceContent == null) throw new ArgumentNullException(nameof(sourceContent));

			_services.Configure<RazorLightOptions>(options => options.DynamicCSharpSources[sourceKey] = sourceContent);
			return AddCSharpSource(sourceKey);
		}
	}
}
