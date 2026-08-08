using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Text;

namespace RazorLight
{
	public sealed class RazorLightOptions
	{
		private HashSet<MetadataReference>? _additionalMetadataReferences;

		public RazorLightOptions()
		{
			Namespaces = new HashSet<string>();
			DynamicTemplates = new ConcurrentDictionary<string, string>();
			CSharpSourceKeys = new HashSet<string>(StringComparer.Ordinal);
			DynamicCSharpSources = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			IncludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			ExcludedAssemblies = new HashSet<string>();
			PageInitializers = new List<Action<ITemplatePage>>();
		}

		public ISet<string> Namespaces { get; set; }

		public IDictionary<string, string> DynamicTemplates { get; set; }

		public ISet<string> CSharpSourceKeys { get; set; }

		public IDictionary<string, string> DynamicCSharpSources { get; set; }

		public HashSet<MetadataReference> AdditionalMetadataReferences
		{
			get => _additionalMetadataReferences ??= new HashSet<MetadataReference>();
			set => _additionalMetadataReferences = value;
		}

		/// <summary>
		/// Assembly names to add to minimal automatic metadata-reference discovery.
		/// </summary>
		public HashSet<string> IncludedAssemblies { get; set; }

		public HashSet<string> ExcludedAssemblies { get; set; }

		/// <summary>
		/// Controls automatic metadata-reference discovery. The default avoids exposing unrelated host
		/// dependencies to compiled templates.
		/// </summary>
		public MetadataReferenceDiscoveryMode MetadataReferenceDiscovery { get; set; }

		internal IList<Action<ITemplatePage>> PageInitializers { get; set; }

		public ICachingProvider? CachingProvider { get; set; }

		public Assembly? OperatingAssembly { get; set; }

		/// <summary>
		/// Transforms expression values before they are written. Defaults to plain text.
		/// </summary>
		public IOutputEncoder OutputEncoder { get; set; } = PlainTextEncoder.Default;

		/// <summary>
		/// Setting this to <c>true</c> provides more information in exceptions.
		/// </summary>
		public bool? EnableDebugMode { get; set; }
	}
}
