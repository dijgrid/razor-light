using Microsoft.Extensions.Primitives;
using System;

namespace RazorLight.Caching
{
	internal interface ICoordinatedCachingProvider
	{
		void PrepareStringTemplate(string templateKey, string cacheKey);

		long BeginCompilation(string templateKey);

		void CompleteCompilation(string templateKey);

		long GetVersion(string templateKey);

		void StoreCompiledTemplate(
			string templateKey,
			string cacheKey,
			Func<ITemplatePage> pageFactory,
			IChangeToken? expirationToken,
			long expectedVersion);
	}
}
