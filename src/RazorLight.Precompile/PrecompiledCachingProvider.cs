using Microsoft.Extensions.Primitives;
using Mono.Cecil;
using RazorLight.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace RazorLight.Precompile
{
	public sealed class PrecompiledCachingProvider : ICachingProvider, IDisposable
	{
		public IReadOnlyDictionary<string, string> Map { get; }
		public IReadOnlyList<string> Diagnostics { get; }
		private readonly MemoryCachingProvider m_cache = new();
		private readonly ConcurrentDictionary<string, string> m_map;

		public PrecompiledCachingProvider(IEnumerable<string> precompiledTemplateFilePaths, StreamWriter? log)
		{
			if (precompiledTemplateFilePaths == null)
			{
				throw new ArgumentNullException(nameof(precompiledTemplateFilePaths));
			}

			var diagnostics = new List<string>();
			var discovered = new SortedDictionary<string, string>(StringComparer.Ordinal);
			foreach (string filePath in precompiledTemplateFilePaths
				.Select(Path.GetFullPath)
				.Distinct(StringComparer.Ordinal)
				.OrderBy(path => path, StringComparer.Ordinal))
			{
				var templateKey = GetPrecompiledTemplateKey(filePath, log, diagnostics);
				if (templateKey == null)
				{
					continue;
				}

				templateKey = NormalizeKey(templateKey);
				if (discovered.TryGetValue(templateKey, out string? duplicatePath))
				{
					throw new RazorLightException(
						$"The key {templateKey} is associated with multiple precompiled templates: " +
						$"'{duplicatePath}' and '{filePath}'.");
				}

				discovered.Add(templateKey, filePath);
			}
			if (discovered.Count == 0)
			{
				throw new RazorLightException("Found no precompiled templates." +
					(diagnostics.Count == 0 ? string.Empty : " " + string.Join(" ", diagnostics)));
			}

			m_map = new ConcurrentDictionary<string, string>(discovered, StringComparer.Ordinal);
			Map = new ReadOnlyDictionary<string, string>(discovered);
			Diagnostics = diagnostics.AsReadOnly();
		}

		private static string? GetPrecompiledTemplateKey(
			string filePath,
			StreamWriter? log,
			ICollection<string> diagnostics)
		{
			try
			{
				using var asmDef = AssemblyDefinition.ReadAssembly(filePath);
				var attributes = asmDef.CustomAttributes
					.Where(o => o.AttributeType.FullName == "RazorLight.Razor.RazorLightTemplateAttribute")
					.ToArray();
				if (attributes.Length > 1)
				{
					throw new RazorLightException(
						$"Assembly '{filePath}' contains multiple RazorLight template attributes.");
				}

				var razorLightAttr = attributes.SingleOrDefault();
				if (razorLightAttr != null)
				{
					var templateKey = razorLightAttr.ConstructorArguments[0].Value as string;
					if (templateKey == null)
					{
						throw new RazorLightException(
							$"Assembly '{filePath}' has a RazorLight template attribute without a string key.");
					}
					log?.WriteLine("GetPrecompiledTemplateKey(\"{0}\") = \"{1}\"", filePath, templateKey);
					return templateKey;
				}
			}
			catch (Exception exception) when (exception is not RazorLightException)
			{
				string diagnostic = $"Skipped assembly '{filePath}': {exception.GetType().Name}: {exception.Message}";
				diagnostics.Add(diagnostic);
				log?.WriteLine(diagnostic);
				return null;
			}

			string message = $"Skipped assembly '{filePath}': no RazorLight template attribute was found.";
			diagnostics.Add(message);
			log?.WriteLine(message);
			return null;
		}

		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken)
		{
			m_cache.CacheTemplate(NormalizeKey(key), pageFactory, expirationToken);
		}

		public bool Contains(string key)
		{
			key = NormalizeKey(key);
			return m_cache.Contains(key) || m_map.ContainsKey(key);
		}

		public void Remove(string key)
		{
			key = NormalizeKey(key);
			m_cache.Remove(key);
			m_map.TryRemove(key, out _);
		}

		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			key = NormalizeKey(key);

			if (m_cache.TryGetTemplate(key, out pageFactory))
			{
				return true;
			}

			if (m_map.TryGetValue(key, out var filePath))
			{
				pageFactory = CreateTemplatePage;
				return true;

				ITemplatePage CreateTemplatePage()
				{
					var templatePageType = FileSystemCachingProvider.GetTemplatePageType(filePath);
					m_cache.CacheTemplate(key, CreateTemplatePage2);
					return CreateTemplatePage2();

					ITemplatePage CreateTemplatePage2() => FileSystemCachingProvider.NewTemplatePage(templatePageType);
				}
			}
			pageFactory = null;
			return false;
		}

		public void Dispose() => m_cache.Dispose();

		private static string NormalizeKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			key = key.Replace('\\', '/');
			return key[0] == '/' ? key : '/' + key;
		}
	}
}
