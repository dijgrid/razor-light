using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Diagnostics.CodeAnalysis;

namespace RazorLight.Caching
{
	public sealed class MemoryCachingProvider : ICachingProvider
	{
		public MemoryCachingProvider()
		{
			var cacheOptions = Options.Create(new MemoryCacheOptions());
			LookupCache = new MemoryCache(cacheOptions);
		}

		private IMemoryCache LookupCache { get; }

		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return LookupCache.TryGetValue(key, out pageFactory);
		}

		public bool Contains(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return LookupCache.TryGetValue(key, out _);
		}

		public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken = null)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			if (pageFactory == null)
			{
				throw new ArgumentNullException(nameof(pageFactory));
			}

			var cacheEntryOptions = new MemoryCacheEntryOptions();
			if (expirationToken != null)
			{
				cacheEntryOptions.ExpirationTokens.Add(expirationToken);
			}

			LookupCache.Set(key, pageFactory, cacheEntryOptions);
		}

		public void Remove(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			LookupCache.Remove(key);
		}
	}
}
