using System;
using System.Threading.Tasks;
using Moq;
using RazorLight.Caching;
using Xunit;

namespace RazorLight.Tests.Caching
{
	public class DefaultCachingProviderTest
	{
		[Fact]
		public void Throws_WhenCachingWithEmptyParams()
		{
			var cache = new MemoryCachingProvider();

			Assert.Throws<ArgumentNullException>(() => cache.CacheTemplate("someKey", null!));
			Assert.Throws<ArgumentNullException>(() => cache.CacheTemplate(null!, GetTestFactory()));
		}

		[Fact]
		public void Throws_OnNullTemplateKey_WhenRetrieve()
		{
			var cache = new MemoryCachingProvider();

			Assert.Throws<ArgumentNullException>(() => cache.TryGetTemplate(null!, out _));
		}

		[Fact]
		public void Ensure_TemplateIsStored()
		{
			var cache = new MemoryCachingProvider();

			string templateKey = "key";
			var factory = GetTestFactory(templateKey);

			cache.CacheTemplate(templateKey, factory);

			Assert.True(cache.TryGetTemplate(templateKey, out var cachedFactory));
			Assert.Equal(factory, cachedFactory);
		}

		[Fact]
		public void Contains_ReturnsTrue_OnCachedTemplate()
		{
			var cache = new MemoryCachingProvider();
			string templateKey = "key";

			cache.CacheTemplate(templateKey, GetTestFactory(templateKey));

			Assert.True(cache.Contains(templateKey));
		}

		[Fact]
		public void Contains_ReturnsFalse_OnNonCachedTemplate()
		{
			var cache = new MemoryCachingProvider();

			Assert.False(cache.Contains("someKey"));
		}

		[Fact]
		public void Returns_EmptyTemplateCacheResult_OnNonExistingTemplate()
		{
			var cache = new MemoryCachingProvider();

			Assert.False(cache.TryGetTemplate("someKey", out var pageFactory));
			Assert.Null(pageFactory);
		}

		[Fact]
		public async Task Applies_OutputEncoder_To_CachedTemplates()
		{
			string templateKey = "Assets.Embedded.Empty.cshtml";

			var encoder = new TemplatePageTest.TestOutputEncoder();
			var engine = new RazorLightEngineBuilder()
				.UseOutputEncoder(encoder)
				.UseMemoryCachingProvider()
				.SetOperatingAssembly(typeof(Root).Assembly)
				.UseEmbeddedResourcesProject(typeof(Root))

				.Build();
			var testCompileToCache = await engine.CompileTemplateAsync(templateKey);

			Assert.Same(encoder, testCompileToCache.OutputEncoder);

			var cachedCompile = await engine.CompileTemplateAsync(templateKey);

			Assert.Same(encoder, cachedCompile.OutputEncoder);

		}

		private Func<ITemplatePage> GetTestFactory(string key = "key")
		{
			var moq = new Mock<ITemplatePage>();

			moq.SetupProperty(t => t.Key, key);

			return new Func<ITemplatePage>(() => moq.Object);
		}
	}
}
