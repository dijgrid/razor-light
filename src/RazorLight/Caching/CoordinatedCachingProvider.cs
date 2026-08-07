using Microsoft.Extensions.Primitives;
using RazorLight.Compilation;
using System;
using System.Collections.Generic;

namespace RazorLight.Caching
{
	internal sealed class CoordinatedCachingProvider : ICachingProvider, ICoordinatedCachingProvider
	{
		private const string CacheKeySeparator = ".__razorlight.";
		private readonly object _sync = new object();
		private readonly ICachingProvider _inner;
		private readonly ITemplateCompilerCache _compilerCache;
		private readonly Dictionary<string, HashSet<string>> _cacheKeys =
			new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		private readonly Dictionary<string, long> _versions =
			new Dictionary<string, long>(StringComparer.Ordinal);

		public CoordinatedCachingProvider(ICachingProvider inner, ITemplateCompilerCache compilerCache)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_compilerCache = compilerCache ?? throw new ArgumentNullException(nameof(compilerCache));
		}

		public TemplateCacheLookupResult RetrieveTemplate(string key)
		{
			string normalizedKey = _compilerCache.NormalizeKey(key);
			TemplateCacheLookupResult result = _inner.RetrieveTemplate(normalizedKey);
			if (result.Success)
			{
				return result;
			}

			return string.Equals(key, normalizedKey, StringComparison.Ordinal)
				? result
				: _inner.RetrieveTemplate(key);
		}

		public bool Contains(string key)
		{
			string normalizedKey = _compilerCache.NormalizeKey(key);
			if (_inner.Contains(normalizedKey))
			{
				return true;
			}

			return !string.Equals(key, normalizedKey, StringComparison.Ordinal) &&
				_inner.Contains(key);
		}

		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (pageFactory == null)
			{
				throw new ArgumentNullException(nameof(pageFactory));
			}

			string templateKey = NormalizeTemplateKey(key);
			lock (_sync)
			{
				IncrementVersion(templateKey);
				_compilerCache.Remove(templateKey);
				RemovePageFactories(templateKey);
				_inner.CacheTemplate(templateKey, pageFactory, expirationToken);
				RegisterCacheKey(templateKey, templateKey);
			}
		}

		public void Remove(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			string templateKey = NormalizeTemplateKey(key);
			lock (_sync)
			{
				IncrementVersion(templateKey);
				_compilerCache.Remove(templateKey);
				RemovePageFactories(templateKey);
				_inner.Remove(key);
				if (!string.Equals(key, templateKey, StringComparison.Ordinal))
				{
					_inner.Remove(templateKey);
				}
			}
		}

		public long GetVersion(string templateKey)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			lock (_sync)
			{
				return _versions.TryGetValue(templateKey, out long version) ? version : 0;
			}
		}

		public void StoreCompiledTemplate(
			string templateKey,
			string cacheKey,
			Func<ITemplatePage> pageFactory,
			IChangeToken? expirationToken,
			long expectedVersion)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			lock (_sync)
			{
				long currentVersion = _versions.TryGetValue(templateKey, out long version) ? version : 0;
				if (currentVersion != expectedVersion)
				{
					return;
				}

				_inner.CacheTemplate(cacheKey, pageFactory, expirationToken);
				RegisterCacheKey(templateKey, cacheKey);
			}
		}

		private string NormalizeTemplateKey(string key)
		{
			int separatorIndex = key.IndexOf(CacheKeySeparator, StringComparison.Ordinal);
			string templateKey = separatorIndex < 0 ? key : key.Substring(0, separatorIndex);
			return _compilerCache.NormalizeKey(templateKey);
		}

		private void IncrementVersion(string templateKey)
		{
			_versions.TryGetValue(templateKey, out long version);
			_versions[templateKey] = checked(version + 1);
		}

		private void RegisterCacheKey(string templateKey, string cacheKey)
		{
			if (!_cacheKeys.TryGetValue(templateKey, out HashSet<string>? keys))
			{
				keys = new HashSet<string>(StringComparer.Ordinal);
				_cacheKeys.Add(templateKey, keys);
			}

			keys.Add(cacheKey);
		}

		private void RemovePageFactories(string templateKey)
		{
			if (!_cacheKeys.Remove(templateKey, out HashSet<string>? keys))
			{
				return;
			}

			foreach (string cacheKey in keys)
			{
				_inner.Remove(cacheKey);
			}
		}
	}
}
