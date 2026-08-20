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
	/// <summary>Represents engine construction settings that are copied when an engine is built.</summary>
	public sealed class RazorLightOptions
	{
		private HashSet<MetadataReference>? _additionalMetadataReferences;

		/// <summary>Creates options with empty collections, minimal reference discovery, and plain-text output.</summary>
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

		/// <summary>Gets or sets namespace imports applied to every generated template.</summary>
		public ISet<string> Namespaces { get; set; }

		/// <summary>Gets or sets keyed in-memory Razor template sources.</summary>
		public IDictionary<string, string> DynamicTemplates { get; set; }

		/// <summary>Gets or sets project C# source keys compiled with every template.</summary>
		public ISet<string> CSharpSourceKeys { get; set; }

		/// <summary>Gets or sets keyed in-memory C# source files compiled with templates.</summary>
		public IDictionary<string, string> DynamicCSharpSources { get; set; }

		/// <summary>Gets or sets explicit Roslyn references available during template compilation.</summary>
		public HashSet<MetadataReference> AdditionalMetadataReferences
		{
			get => _additionalMetadataReferences ??= new HashSet<MetadataReference>();
			set => _additionalMetadataReferences = value;
		}

		/// <summary>
		/// Assembly names to add to minimal automatic metadata-reference discovery.
		/// </summary>
		public HashSet<string> IncludedAssemblies { get; set; }

		/// <summary>Gets or sets assembly names excluded from automatic metadata-reference discovery.</summary>
		public HashSet<string> ExcludedAssemblies { get; set; }

		/// <summary>
		/// Controls automatic metadata-reference discovery. The default avoids exposing unrelated host
		/// dependencies to compiled templates.
		/// </summary>
		public MetadataReferenceDiscoveryMode MetadataReferenceDiscovery { get; set; }

		internal IList<Action<ITemplatePage>> PageInitializers { get; set; }

		/// <summary>Gets or sets the compiled page cache. A supplied provider remains caller-owned.</summary>
		public ICachingProvider? CachingProvider { get; set; }

		/// <summary>Gets or sets the assembly whose dependency context supplies compilation references.</summary>
		public Assembly? OperatingAssembly { get; set; }

		/// <summary>
		/// Transforms expression values before they are written. Defaults to plain text.
		/// </summary>
		public IOutputEncoder OutputEncoder { get; set; } = PlainTextEncoder.Default;

		/// <summary>
		/// Setting this to <c>true</c> provides more information in exceptions.
		/// </summary>
		public bool? EnableDebugMode { get; set; }

		/// <summary>
		/// Gets or sets whether compiler diagnostic messages are replaced with diagnostic IDs. Mapped paths and
		/// generated-source details remain controlled separately by <see cref="EnableDebugMode"/>.
		/// </summary>
		public bool RedactCompilerDiagnosticMessages { get; set; }
	}
}
