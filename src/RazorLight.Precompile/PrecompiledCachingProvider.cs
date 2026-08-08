using Microsoft.Extensions.Primitives;
using Mono.Cecil;
using RazorLight.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace RazorLight.Precompile
{
	public sealed class PrecompiledCachingProvider : ICachingProvider
	{
		public readonly IReadOnlyDictionary<string, string> Map;
		private readonly MemoryCachingProvider m_cache = new();
		private readonly ConcurrentDictionary<string, string> m_map;

		public PrecompiledCachingProvider(IEnumerable<string> precompiledTemplateFilePaths, StreamWriter? log)
		{
			m_map = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
			foreach (var filePath in precompiledTemplateFilePaths)
			{
				var templateKey = GetPrecompiledTemplateKey(filePath, log);
				if (templateKey == null)
				{
					continue;
				}

				templateKey = NormalizeKey(templateKey);
				if (!m_map.TryAdd(templateKey, filePath))
				{
					string dupe = m_map[templateKey];
					throw new RazorLightException($"The key {templateKey} is associated with at least two precompiled templates - {dupe} and {filePath}");
				}
			}
			if (m_map.Count == 0)
			{
				throw new RazorLightException($"Found no precompiled templates.");
			}
			Map = m_map;
		}

		private static string? GetPrecompiledTemplateKey(string filePath, StreamWriter? log)
		{
			try
			{
				using var asmDef = AssemblyDefinition.ReadAssembly(filePath);
				var razorLightAttr = asmDef.CustomAttributes.SingleOrDefault(o => o.AttributeType.FullName == "RazorLight.Razor.RazorLightTemplateAttribute");
				if (razorLightAttr != null)
				{
					var templateKey = razorLightAttr.ConstructorArguments[0].Value as string;
					if (templateKey == null)
					{
						return null;
					}
					log?.WriteLine("GetPrecompiledTemplateKey(\"{0}\") = \"{1}\"", filePath, templateKey);
					return templateKey;
				}
			}
			catch { }
			log?.WriteLine("GetPrecompiledTemplateKey(\"{0}\") = null", filePath);
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
			throw new RazorLightException($"No precompiled template found for the key {key}");
		}

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
