using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace RazorLight.Caching
{
	public interface ICachingProvider
	{
		bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory);

		void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken);

		bool Contains(string key);

		void Remove(string key);
	}
}
