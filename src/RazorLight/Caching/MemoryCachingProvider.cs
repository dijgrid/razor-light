using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace RazorLight.Caching
{
	/// <summary>Stores compiled page factories in a thread-safe process-local memory cache.</summary>
	public sealed class MemoryCachingProvider : ICachingProvider, IDisposable
	{
		/// <summary>Creates an empty memory caching provider.</summary>
		public MemoryCachingProvider()
		{
			var cacheOptions = Options.Create(new MemoryCacheOptions());
			LookupCache = new MemoryCache(cacheOptions);
		}

		private IMemoryCache LookupCache { get; }
		internal bool IsDisposed { get; private set; }

		/// <inheritdoc />
		public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return LookupCache.TryGetValue(key, out pageFactory);
		}

		/// <inheritdoc />
		public bool Contains(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			return LookupCache.TryGetValue(key, out _);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public void Remove(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}

			LookupCache.Remove(key);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (IsDisposed) return;
			LookupCache.Dispose();
			IsDisposed = true;
		}
	}
}
