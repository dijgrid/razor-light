using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RazorLight.Razor;

namespace RazorLight.Caching
{
	/// <summary>Loads trusted RazorLight template assemblies into a cache for precompiled-only execution.</summary>
	public sealed class PrecompiledCachingProvider : ICachingProvider, IDisposable
	{
		private readonly MemoryCachingProvider _runtimeCache = new MemoryCachingProvider();
		private readonly ConcurrentDictionary<string, string> _paths;
		private readonly ConcurrentDictionary<string, Type> _templateTypes;

		[RequiresDynamicCode("Loading separately deployed precompiled assemblies requires dynamic assembly loading.")]
		[RequiresUnreferencedCode("Precompiled template page types and constructors must be preserved by the build pipeline.")]
		public PrecompiledCachingProvider(IEnumerable<string> assemblyPaths, TextWriter? log = null)
			: this(ReadInputs(assemblyPaths), log)
		{
		}

		[RequiresDynamicCode("Loading separately deployed precompiled assemblies requires dynamic assembly loading.")]
		[RequiresUnreferencedCode("Precompiled template page types and constructors must be preserved by the build pipeline.")]
		public static async Task<PrecompiledCachingProvider> CreateAsync(
			IEnumerable<string> assemblyPaths,
			TextWriter? log = null,
			CancellationToken cancellationToken = default)
		{
			if (assemblyPaths == null) throw new ArgumentNullException(nameof(assemblyPaths));
			var inputs = new List<AssemblyInput>();
			foreach (string path in NormalizePaths(assemblyPaths))
			{
				cancellationToken.ThrowIfCancellationRequested();
				byte[] assemblyBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
				string pdbPath = Path.ChangeExtension(path, ".pdb");
				byte[]? pdbBytes = File.Exists(pdbPath)
					? await File.ReadAllBytesAsync(pdbPath, cancellationToken).ConfigureAwait(false)
					: null;
				inputs.Add(new AssemblyInput(path, assemblyBytes, pdbBytes));
			}

			return new PrecompiledCachingProvider(inputs, log);
		}

		[RequiresDynamicCode("Loading separately deployed precompiled assemblies requires dynamic assembly loading.")]
		[RequiresUnreferencedCode("Precompiled template page types and constructors must be preserved by the build pipeline.")]
		private PrecompiledCachingProvider(IEnumerable<AssemblyInput> inputs, TextWriter? log)
		{
			var diagnostics = new List<string>();
			var paths = new SortedDictionary<string, string>(StringComparer.Ordinal);
			var types = new Dictionary<string, Type>(StringComparer.Ordinal);
			foreach (AssemblyInput input in inputs)
			{
				RazorLightTemplateAttribute? metadata = Inspect(input, log, diagnostics);
				if (metadata == null) continue;

				string key = NormalizeKey(metadata.Key);
				if (paths.TryGetValue(key, out string? duplicate))
				{
					throw new RazorLightException(
						$"The key {key} is associated with multiple precompiled templates: '{duplicate}' and '{input.Path}'.");
				}

				paths.Add(key, input.Path);
				types.Add(key, metadata.TemplateType);
			}

			if (paths.Count == 0)
			{
				throw new RazorLightException("Found no precompiled templates." +
					(diagnostics.Count == 0 ? string.Empty : " " + string.Join(" ", diagnostics)));
			}

			_paths = new ConcurrentDictionary<string, string>(paths, StringComparer.Ordinal);
			_templateTypes = new ConcurrentDictionary<string, Type>(types, StringComparer.Ordinal);
			Map = new ReadOnlyDictionary<string, string>(paths);
			Diagnostics = diagnostics.AsReadOnly();
		}

		public IReadOnlyDictionary<string, string> Map { get; }
		public IReadOnlyList<string> Diagnostics { get; }

		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken) =>
			_runtimeCache.CacheTemplate(NormalizeKey(key), pageFactory, expirationToken);

		public bool Contains(string key)
		{
			key = NormalizeKey(key);
			return _runtimeCache.Contains(key) || _paths.ContainsKey(key);
		}

		public void Remove(string key)
		{
			key = NormalizeKey(key);
			_runtimeCache.Remove(key);
			_paths.TryRemove(key, out _);
			_templateTypes.TryRemove(key, out _);
		}

		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			key = NormalizeKey(key);
			if (_runtimeCache.TryGetTemplate(key, out pageFactory)) return true;
			if (!_templateTypes.TryGetValue(key, out Type? pageType))
			{
				pageFactory = null;
				return false;
			}

			pageFactory = () => FileSystemCachingProvider.NewTemplatePage(pageType);
			_runtimeCache.CacheTemplate(key, pageFactory);
			return true;
		}

		public void Dispose() => _runtimeCache.Dispose();

		[RequiresDynamicCode("Loading separately deployed precompiled assemblies requires dynamic assembly loading.")]
		[RequiresUnreferencedCode("Precompiled template page types and constructors must be preserved by the build pipeline.")]
		private static RazorLightTemplateAttribute? Inspect(
			AssemblyInput input,
			TextWriter? log,
			ICollection<string> diagnostics)
		{
			string path = input.Path;
			try
			{
				Assembly assembly = Assembly.Load(input.AssemblyBytes, input.PdbBytes);
				CustomAttributeData[] records = assembly.GetCustomAttributesData()
					.Where(item => item.AttributeType == typeof(RazorLightTemplateAttribute))
					.ToArray();
				if (records.Length > 1)
				{
					throw new RazorLightException($"Assembly '{path}' contains multiple RazorLight template attributes.");
				}
				if (records.Length == 1 && records[0].ConstructorArguments.Count < 6)
				{
					throw new RazorLightException(
						$"Assembly '{path}' uses legacy RazorLight template metadata and must be recompiled.");
				}

				RazorLightTemplateAttribute? metadata = assembly.GetCustomAttributes<RazorLightTemplateAttribute>().SingleOrDefault();
				if (metadata == null)
				{
					string missing = $"Skipped assembly '{path}': no RazorLight template attribute was found.";
					diagnostics.Add(missing);
					log?.WriteLine(missing);
					return null;
				}

				ValidateMetadata(path, metadata);
				log?.WriteLine("Precompiled template '{0}' = '{1}'", metadata.Key, path);
				return metadata;
			}
			catch (Exception exception) when (exception is not RazorLightException)
			{
				string diagnostic = $"Skipped assembly '{path}': {exception.GetType().Name}: {exception.Message}";
				diagnostics.Add(diagnostic);
				log?.WriteLine(diagnostic);
				return null;
			}
		}

		private static void ValidateMetadata(string path, RazorLightTemplateAttribute metadata)
		{
			if (metadata.FormatVersion != RazorLightTemplateAttribute.CurrentFormatVersion ||
				metadata.CompilerVersion != RazorLightTemplateAttribute.CurrentCompilerVersion)
			{
				throw new RazorLightException(
					$"Assembly '{path}' was produced for template format {metadata.FormatVersion} and compiler " +
					$"'{metadata.CompilerVersion}', but this runtime requires format {RazorLightTemplateAttribute.CurrentFormatVersion} " +
					$"and compiler '{RazorLightTemplateAttribute.CurrentCompilerVersion}'. Recompile the template.");
			}
			if (string.IsNullOrWhiteSpace(metadata.Key) || string.IsNullOrWhiteSpace(metadata.ModelContract) ||
				string.IsNullOrWhiteSpace(metadata.SourceChecksum))
			{
				throw new RazorLightException($"Assembly '{path}' has incomplete RazorLight template metadata.");
			}
			if (!typeof(ITemplatePage).IsAssignableFrom(metadata.TemplateType))
			{
				throw new RazorLightException($"Assembly '{path}' names a template type that does not implement ITemplatePage.");
			}
		}

		private static string NormalizeKey(string key)
		{
			if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
			key = key.Replace('\\', '/');
			return key[0] == '/' ? key : '/' + key;
		}

		private static IEnumerable<string> NormalizePaths(IEnumerable<string> assemblyPaths)
		{
			if (assemblyPaths == null) throw new ArgumentNullException(nameof(assemblyPaths));
			return assemblyPaths.Select(Path.GetFullPath).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal);
		}

		private static IEnumerable<AssemblyInput> ReadInputs(IEnumerable<string> assemblyPaths)
		{
			foreach (string path in NormalizePaths(assemblyPaths))
			{
				string pdbPath = Path.ChangeExtension(path, ".pdb");
				yield return new AssemblyInput(
					path,
					File.ReadAllBytes(path),
					File.Exists(pdbPath) ? File.ReadAllBytes(pdbPath) : null);
			}
		}

		private sealed record AssemblyInput(string Path, byte[] AssemblyBytes, byte[]? PdbBytes);
	}
}
