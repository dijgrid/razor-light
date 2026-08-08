using Microsoft.Extensions.Primitives;
using RazorLight.Compilation;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace RazorLight.Caching
{
	internal sealed class CoordinatedCachingProvider : ICachingProvider, ICoordinatedCachingProvider
	{
		private readonly ICachingProvider _inner;
		private readonly ITemplateCompilerCache _compilerCache;
		private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _cacheKeys =
			new ConcurrentDictionary<string, ConcurrentDictionary<string, byte>>(StringComparer.Ordinal);
		private readonly ConcurrentDictionary<string, long> _versions =
			new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
		private readonly ConcurrentDictionary<string, int> _activeCompilations =
			new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
		private readonly ConcurrentDictionary<string, string> _stringTemplateCacheKeys =
			new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

		public CoordinatedCachingProvider(ICachingProvider inner, ITemplateCompilerCache compilerCache)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
			_compilerCache = compilerCache ?? throw new ArgumentNullException(nameof(compilerCache));
		}

		internal int TrackedVersionCount => _versions.Count;
		internal int TrackedTemplateCount => _cacheKeys.Count;

		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			string normalizedKey = _compilerCache.NormalizeKey(key);
			if (_inner.TryGetTemplate(normalizedKey, out pageFactory))
			{
				return true;
			}

			return !string.Equals(key, normalizedKey, StringComparison.Ordinal) &&
				_inner.TryGetTemplate(key, out pageFactory);
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
			IncrementVersion(templateKey);
			_compilerCache.Remove(templateKey);
			RemovePageFactories(templateKey);
			_inner.CacheTemplate(templateKey, pageFactory, expirationToken);
			RegisterCacheKey(templateKey, templateKey, expirationToken);
		}

		public void Remove(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			string templateKey = NormalizeTemplateKey(key);
			IncrementVersion(templateKey);
			_compilerCache.Remove(templateKey);
			RemovePageFactories(templateKey);
			_inner.Remove(key);
			if (!string.Equals(key, templateKey, StringComparison.Ordinal))
			{
				_inner.Remove(templateKey);
			}
			_stringTemplateCacheKeys.TryRemove(templateKey, out _);
			TryRemoveUnusedVersion(templateKey);
		}

		public void PrepareStringTemplate(string templateKey, string cacheKey)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			if (_stringTemplateCacheKeys.TryGetValue(templateKey, out string? previousCacheKey) &&
				string.Equals(previousCacheKey, cacheKey, StringComparison.Ordinal))
			{
				return;
			}

			_stringTemplateCacheKeys[templateKey] = cacheKey;
			IncrementVersion(templateKey);
			_compilerCache.Remove(templateKey);
			RemovePageFactories(templateKey);
			TryRemoveUnusedVersion(templateKey);
		}

		public long BeginCompilation(string templateKey)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			_activeCompilations.AddOrUpdate(templateKey, 1, (_, count) => checked(count + 1));
			return GetVersion(templateKey);
		}

		public void CompleteCompilation(string templateKey)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			_activeCompilations.AddOrUpdate(templateKey, 0, (_, count) => Math.Max(0, count - 1));
			if (_activeCompilations.TryGetValue(templateKey, out int count) && count == 0)
			{
				_activeCompilations.TryRemove(new KeyValuePair<string, int>(templateKey, 0));
			}

			TryRemoveUnusedVersion(templateKey);
		}

		public long GetVersion(string templateKey)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			return _versions.TryGetValue(templateKey, out long version) ? version : 0;
		}

		public void StoreCompiledTemplate(
			string templateKey,
			string cacheKey,
			Func<ITemplatePage> pageFactory,
			IChangeToken? expirationToken,
			long expectedVersion)
		{
			templateKey = NormalizeTemplateKey(templateKey);
			long currentVersion = _versions.TryGetValue(templateKey, out long version) ? version : 0;
			if (currentVersion != expectedVersion)
			{
				return;
			}

			_inner.CacheTemplate(cacheKey, pageFactory, expirationToken);
			if ((_versions.TryGetValue(templateKey, out version) ? version : 0) != expectedVersion)
			{
				_inner.Remove(cacheKey);
				return;
			}

			RegisterCacheKey(templateKey, cacheKey, expirationToken);
		}

		private string NormalizeTemplateKey(string key)
		{
			return _compilerCache.NormalizeKey(key);
		}

		private void IncrementVersion(string templateKey)
		{
			_versions.AddOrUpdate(templateKey, 1, (_, version) => checked(version + 1));
		}

		private void RegisterCacheKey(string templateKey, string cacheKey, IChangeToken? expirationToken)
		{
			var keys = _cacheKeys.GetOrAdd(
				templateKey,
				_ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));
			keys[cacheKey] = 0;
			if (expirationToken != null)
			{
				expirationToken.RegisterChangeCallback(
					_ => UnregisterCacheKey(templateKey, cacheKey),
					state: null);
			}
		}

		private void RemovePageFactories(string templateKey)
		{
			if (!_cacheKeys.TryRemove(templateKey, out ConcurrentDictionary<string, byte>? keys))
			{
				return;
			}

			foreach (string cacheKey in keys.Keys)
			{
				_inner.Remove(cacheKey);
			}
		}

		private void UnregisterCacheKey(string templateKey, string cacheKey)
		{
			if (!_cacheKeys.TryGetValue(templateKey, out ConcurrentDictionary<string, byte>? keys))
			{
				return;
			}

			keys.TryRemove(cacheKey, out _);
			if (keys.IsEmpty)
			{
				_cacheKeys.TryRemove(new KeyValuePair<string, ConcurrentDictionary<string, byte>>(templateKey, keys));
				_stringTemplateCacheKeys.TryRemove(
					new KeyValuePair<string, string>(templateKey, cacheKey));
				TryRemoveUnusedVersion(templateKey);
			}
		}

		private void TryRemoveUnusedVersion(string templateKey)
		{
			if (_activeCompilations.ContainsKey(templateKey) ||
				_cacheKeys.ContainsKey(templateKey) ||
				_stringTemplateCacheKeys.ContainsKey(templateKey) ||
				!_versions.TryGetValue(templateKey, out long version))
			{
				return;
			}

			_versions.TryRemove(new KeyValuePair<string, long>(templateKey, version));
		}
	}
}
