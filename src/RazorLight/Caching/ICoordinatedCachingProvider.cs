using Microsoft.Extensions.Primitives;
using System;

namespace RazorLight.Caching
{
	internal interface ICoordinatedCachingProvider
	{
		long GetVersion(string templateKey);

		void StoreCompiledTemplate(
			string templateKey,
			string cacheKey,
			Func<ITemplatePage> pageFactory,
			IChangeToken? expirationToken,
			long expectedVersion);
	}
}
