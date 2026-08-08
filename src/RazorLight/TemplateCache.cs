using System;
using RazorLight.Caching;

namespace RazorLight
{
	internal sealed class TemplateCache
	{
		private readonly ICachingProvider? _provider;

		public TemplateCache(ICachingProvider? provider)
		{
			_provider = provider;
		}

		public bool Contains(string key)
		{
			ValidateKey(key);
			return _provider?.Contains(key) ?? false;
		}

		public void Remove(string key)
		{
			ValidateKey(key);
			_provider?.Remove(key);
		}

		private static void ValidateKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentNullException(nameof(key));
			}
		}
	}
}
