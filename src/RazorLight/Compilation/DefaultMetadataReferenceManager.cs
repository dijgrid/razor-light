using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using System.Linq;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Options;

namespace RazorLight.Compilation
{
	public class DefaultMetadataReferenceManager : IMetadataReferenceManager
	{
		private readonly IAssemblyPathFormatter _pathFormatter = new DefaultAssemblyPathFormatter();
		public HashSet<MetadataReference> AdditionalMetadataReferences { get; }
		internal HashSet<string> IncludedAssemblies { get; }
		public HashSet<string> ExcludedAssemblies { get; }
		internal MetadataReferenceDiscoveryMode DiscoveryMode { get; }

		public DefaultMetadataReferenceManager()
		{
			AdditionalMetadataReferences = new HashSet<MetadataReference>();
			IncludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ExcludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			DiscoveryMode = MetadataReferenceDiscoveryMode.Minimal;
		}

		public DefaultMetadataReferenceManager(IOptions<RazorLightOptions> options, IAssemblyPathFormatter pathFormatter) : this(
			options.Value.AdditionalMetadataReferences,
			options.Value.IncludedAssemblies,
			options.Value.ExcludedAssemblies,
			options.Value.MetadataReferenceDiscovery)
		{
			_pathFormatter = pathFormatter;
		}

		public DefaultMetadataReferenceManager(HashSet<MetadataReference> metadataReferences)
		{
			AdditionalMetadataReferences = metadataReferences ?? throw new ArgumentNullException(nameof(metadataReferences));
			IncludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ExcludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			DiscoveryMode = MetadataReferenceDiscoveryMode.Minimal;
		}

		public DefaultMetadataReferenceManager(HashSet<MetadataReference> metadataReferences, HashSet<string> excludedAssemblies)
		{
			AdditionalMetadataReferences = metadataReferences ?? throw new ArgumentNullException(nameof(metadataReferences));
			IncludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ExcludedAssemblies = new HashSet<string>(
				excludedAssemblies ?? throw new ArgumentNullException(nameof(excludedAssemblies)),
				StringComparer.OrdinalIgnoreCase);
			DiscoveryMode = MetadataReferenceDiscoveryMode.Minimal;
		}

		internal DefaultMetadataReferenceManager(
			HashSet<MetadataReference> metadataReferences,
			HashSet<string> includedAssemblies,
			HashSet<string> excludedAssemblies,
			MetadataReferenceDiscoveryMode discoveryMode)
		{
			AdditionalMetadataReferences = metadataReferences ?? throw new ArgumentNullException(nameof(metadataReferences));
			IncludedAssemblies = new HashSet<string>(
				includedAssemblies ?? throw new ArgumentNullException(nameof(includedAssemblies)),
				StringComparer.OrdinalIgnoreCase);
			ExcludedAssemblies = new HashSet<string>(
				excludedAssemblies ?? throw new ArgumentNullException(nameof(excludedAssemblies)),
				StringComparer.OrdinalIgnoreCase);
			DiscoveryMode = discoveryMode;
		}

		public IReadOnlyList<MetadataReference> Resolve(Assembly assembly)
		{
			var dependencyContext = DependencyContext.Load(assembly);

			return Resolve(assembly, dependencyContext);
		}

		internal IReadOnlyList<MetadataReference> Resolve(Assembly assembly, DependencyContext? dependencyContext)
		{
			var libraryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			IEnumerable<string> references;
			HashSet<string> allowedAssemblies = GetAllowedAssemblyNames(assembly);
			if (dependencyContext == null)
			{
				IEnumerable<Assembly> discoveredAssemblies = DiscoveryMode == MetadataReferenceDiscoveryMode.All
					? GetReferencedAssemblies(assembly, ExcludedAssemblies).Union(new[] { assembly })
					: GetMinimalAssemblies(assembly, allowedAssemblies);
				references = discoveredAssemblies.Select(p => _pathFormatter.GetAssemblyPath(p)).ToList();
			}
			else
			{
				references = dependencyContext.CompileLibraries
					.SelectMany(library => library.ResolveReferencePaths()
						.Where(path => IsAutomaticallyDiscovered(path, allowedAssemblies, library.Type)))
					.ToList();
			}

			var metadataReferences = new List<MetadataReference>();

			foreach (var reference in references)
			{
				if (string.IsNullOrWhiteSpace(reference) ||
					!File.Exists(reference) ||
					!libraryPaths.Add(reference))
				{
					continue;
				}

				using (var stream = File.OpenRead(reference))
				{
					var moduleMetadata = ModuleMetadata.CreateFromStream(stream, PEStreamOptions.PrefetchMetadata);
					var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);

					metadataReferences.Add(assemblyMetadata.GetReference(filePath: reference));
				}
			}

			if (AdditionalMetadataReferences.Any())
			{
				metadataReferences.AddRange(AdditionalMetadataReferences);
			}

			if (metadataReferences.Count == 0)
			{
				throw new RazorLightException(
					"No usable metadata references were found for runtime template compilation. " +
					"Make sure PreserveCompilationContext is set to true in the application's project file, " +
					"or provide explicit references with AddMetadataReferences.");
			}

			return metadataReferences;
		}

		private bool IsAutomaticallyDiscovered(string path, ISet<string> allowedAssemblies, string libraryType)
		{
			string assemblyName = Path.GetFileNameWithoutExtension(path);
			if (ExcludedAssemblies.Contains(assemblyName))
			{
				return false;
			}

			return DiscoveryMode == MetadataReferenceDiscoveryMode.All ||
				string.Equals(libraryType, "project", StringComparison.OrdinalIgnoreCase) ||
				allowedAssemblies.Contains(assemblyName);
		}

		private HashSet<string> GetAllowedAssemblyNames(Assembly operatingAssembly)
		{
			var allowed = new HashSet<string>(IncludedAssemblies, StringComparer.OrdinalIgnoreCase);
			AddAssemblyName(allowed, operatingAssembly);

			Assembly razorLightAssembly = typeof(RazorLightEngine).Assembly;
			AddAssemblyName(allowed, razorLightAssembly);
			foreach (Assembly referencedAssembly in GetReferencedAssemblies(razorLightAssembly, ExcludedAssemblies))
			{
				AddAssemblyName(allowed, referencedAssembly);
			}

			return allowed;
		}

		private static IEnumerable<Assembly> GetMinimalAssemblies(Assembly operatingAssembly, ISet<string> allowedAssemblies)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(candidate => candidate.GetName().Name is string name && allowedAssemblies.Contains(name))
				.ToList();
			if (!assemblies.Contains(operatingAssembly))
			{
				assemblies.Add(operatingAssembly);
			}

			return assemblies;
		}

		private static void AddAssemblyName(ISet<string> names, Assembly assembly)
		{
			string? name = assembly.GetName().Name;
			if (!string.IsNullOrEmpty(name))
			{
				names.Add(name);
			}
		}

		private static IEnumerable<Assembly> GetReferencedAssemblies(Assembly a, ISet<string> excludedAssemblies, HashSet<string>? visitedAssemblies = null)
		{
			visitedAssemblies = visitedAssemblies ?? new HashSet<string>();
			if (!visitedAssemblies.Add(a.FullName ?? a.GetName().Name ?? a.ToString()))
			{
				yield break;
			}

			foreach (var assemblyRef in a.GetReferencedAssemblies())
			{
				if (visitedAssemblies.Contains(assemblyRef.FullName)) { continue; }

				if (assemblyRef.Name != null && excludedAssemblies.Contains(assemblyRef.Name)) { continue; }
				var loadedAssembly = Assembly.Load(assemblyRef);
				yield return loadedAssembly;
				foreach (var referenced in GetReferencedAssemblies(loadedAssembly, excludedAssemblies, visitedAssemblies))
				{
					yield return referenced;
				}
			}
		}

	
	}
}
