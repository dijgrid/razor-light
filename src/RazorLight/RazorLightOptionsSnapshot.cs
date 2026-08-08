using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

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
	}
}
