using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RazorLight
{
	internal sealed class RazorLightOptionsSnapshot
	{
		private RazorLightOptionsSnapshot(RazorLightOptions options)
		{
			Options = options;
		}

		public RazorLightOptions Options { get; }

		public static RazorLightOptionsSnapshot Create(RazorLightOptions source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			ValidateCollections(source);

			return new RazorLightOptionsSnapshot(new RazorLightOptions
			{
				Namespaces = new HashSet<string>(source.Namespaces),
				DynamicTemplates = new ConcurrentDictionary<string, string>(source.DynamicTemplates),
				CSharpSourceKeys = new HashSet<string>(source.CSharpSourceKeys, StringComparer.Ordinal),
				DynamicCSharpSources = new ConcurrentDictionary<string, string>(source.DynamicCSharpSources, StringComparer.Ordinal),
				AdditionalMetadataReferences = new HashSet<MetadataReference>(source.AdditionalMetadataReferences),
				IncludedAssemblies = new HashSet<string>(source.IncludedAssemblies, StringComparer.OrdinalIgnoreCase),
				ExcludedAssemblies = new HashSet<string>(source.ExcludedAssemblies),
				MetadataReferenceDiscovery = source.MetadataReferenceDiscovery,
				PageInitializers = new List<Action<ITemplatePage>>(source.PageInitializers),
				CachingProvider = source.CachingProvider,
				OperatingAssembly = source.OperatingAssembly,
				OutputEncoder = source.OutputEncoder,
				EnableDebugMode = source.EnableDebugMode,
			});
		}

		public static RazorLightOptionsSnapshot CreatePrecompiled(RazorLightOptions source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (source.Namespaces == null) throw new RazorLightException("RazorLightOptions.Namespaces cannot be null.");
			if (source.DynamicTemplates == null) throw new RazorLightException("RazorLightOptions.DynamicTemplates cannot be null.");
			if (source.CSharpSourceKeys == null) throw new RazorLightException("RazorLightOptions.CSharpSourceKeys cannot be null.");
			if (source.DynamicCSharpSources == null) throw new RazorLightException("RazorLightOptions.DynamicCSharpSources cannot be null.");
			if (source.PageInitializers == null) throw new RazorLightException("RazorLightOptions.PageInitializers cannot be null.");
			if (source.OutputEncoder == null) throw new RazorLightException("RazorLightOptions.OutputEncoder cannot be null.");

			return new RazorLightOptionsSnapshot(new RazorLightOptions
			{
				Namespaces = new HashSet<string>(source.Namespaces),
				DynamicTemplates = new ConcurrentDictionary<string, string>(source.DynamicTemplates),
				CSharpSourceKeys = new HashSet<string>(source.CSharpSourceKeys, StringComparer.Ordinal),
				DynamicCSharpSources = new ConcurrentDictionary<string, string>(source.DynamicCSharpSources, StringComparer.Ordinal),
				PageInitializers = new List<Action<ITemplatePage>>(source.PageInitializers),
				CachingProvider = source.CachingProvider,
				OutputEncoder = source.OutputEncoder,
				EnableDebugMode = source.EnableDebugMode,
			});
		}

		private static void ValidateCollections(RazorLightOptions options)
		{
			if (options.Namespaces == null) throw new RazorLightException("RazorLightOptions.Namespaces cannot be null.");
			if (options.DynamicTemplates == null) throw new RazorLightException("RazorLightOptions.DynamicTemplates cannot be null.");
			if (options.CSharpSourceKeys == null) throw new RazorLightException("RazorLightOptions.CSharpSourceKeys cannot be null.");
			if (options.DynamicCSharpSources == null) throw new RazorLightException("RazorLightOptions.DynamicCSharpSources cannot be null.");
			if (options.AdditionalMetadataReferences == null) throw new RazorLightException("RazorLightOptions.AdditionalMetadataReferences cannot be null.");
			if (options.IncludedAssemblies == null) throw new RazorLightException("RazorLightOptions.IncludedAssemblies cannot be null.");
			if (options.ExcludedAssemblies == null) throw new RazorLightException("RazorLightOptions.ExcludedAssemblies cannot be null.");
			if (options.PageInitializers == null) throw new RazorLightException("RazorLightOptions.PageInitializers cannot be null.");
			if (options.OutputEncoder == null) throw new RazorLightException("RazorLightOptions.OutputEncoder cannot be null.");
		}
	}
}
