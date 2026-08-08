using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Razor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RazorLight
{
	public class RazorLightDependencyBuilder
	{
		private readonly IServiceCollection _services;
		public RazorLightDependencyBuilder(IServiceCollection services)
		{
			_services = services;
		}

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

		public RazorLightDependencyBuilder UseMemoryCachingProvider()
		{
			_services.RemoveAll<ICachingProvider>();
			_services.AddSingleton<ICachingProvider, MemoryCachingProvider>();
			return this;
		}

		public RazorLightDependencyBuilder UseEmbeddedResourcesProject(Type rootType)
		{
			_services.RemoveAll<RazorLightProject>();
			RazorLightProject project = new EmbeddedRazorProject(rootType);
			// ReSharper disable once RedundantTypeArgumentsOfMethod
			_services.AddSingleton<RazorLightProject>(project);
			return this;
		}

		public RazorLightDependencyBuilder SetOperatingAssembly(Assembly assembly)
		{
			_services.Configure<RazorLightOptions>(x => x.OperatingAssembly = assembly);
			return this;
		}

		public RazorLightDependencyBuilder ExcludeAssemblies(params string[] assemblyNames)
		{
			var excludedAssemblies = new HashSet<string>();

			foreach (var assemblyName in assemblyNames)
			{
				excludedAssemblies.Add(assemblyName);
			}

			_services.Configure<RazorLightOptions>(x => x.ExcludedAssemblies = excludedAssemblies);
			return this;
		}

		public RazorLightDependencyBuilder IncludeAssemblies(params string[] assemblyNames)
		{
			if (assemblyNames == null)
			{
				throw new ArgumentNullException(nameof(assemblyNames));
			}

			var includedAssemblies = new HashSet<string>(assemblyNames, StringComparer.OrdinalIgnoreCase);
			_services.Configure<RazorLightOptions>(x => x.IncludedAssemblies = includedAssemblies);
			return this;
		}

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

		public RazorLightDependencyBuilder AddMetadataReferences(params MetadataReference[] metadata)
		{
			var metadataReferences = new HashSet<MetadataReference>();

			foreach (var reference in metadata)
			{
				metadataReferences.Add(reference);
			}
			_services.Configure<RazorLightOptions>(x => x.AdditionalMetadataReferences = metadataReferences);
			return this;
		}

		public RazorLightDependencyBuilder AddCSharpSource(string sourceKey)
		{
			if (string.IsNullOrWhiteSpace(sourceKey))
			{
				throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			}

			_services.Configure<RazorLightOptions>(options => options.CSharpSourceKeys.Add(sourceKey));
			return this;
		}

		public RazorLightDependencyBuilder AddCSharpSource(string sourceKey, string sourceContent)
		{
			if (string.IsNullOrWhiteSpace(sourceKey)) throw new ArgumentException("A C# source key is required.", nameof(sourceKey));
			if (sourceContent == null) throw new ArgumentNullException(nameof(sourceContent));

			_services.Configure<RazorLightOptions>(options => options.DynamicCSharpSources[sourceKey] = sourceContent);
			return AddCSharpSource(sourceKey);
		}
	}
}
