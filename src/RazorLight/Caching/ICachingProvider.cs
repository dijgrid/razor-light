using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace RazorLight.Caching
{
	/// <summary>
	/// Stores compiled page factories by normalized template identity. Implementations must support
	/// concurrent lookup, replacement, inspection, and removal.
	/// </summary>
	public interface ICachingProvider
	{
		/// <summary>Attempts to retrieve a factory that creates a fresh mutable template page.</summary>
		bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory);

		/// <summary>Adds or replaces a page factory and its optional dependency change token.</summary>
		void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken);

		/// <summary>Returns whether the normalized template identity is currently cached.</summary>
		bool Contains(string key);

		/// <summary>Removes a cached identity. Removing an unknown identity is safe.</summary>
		void Remove(string key);
	}
}
