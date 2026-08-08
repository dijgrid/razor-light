using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Razor;
using RazorLight.Tests.Integration;
using Xunit;

namespace RazorLight.Tests.Caching
{
	[Collection(NonParallelRazorCompilationCollection.Name)]
	public class CacheCoherenceTest
	{
		[Fact]
		public async Task Cache_Facade_Inspects_And_Invalidates_String_Templates()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.UseMemoryCachingProvider()
				.Build();

			Assert.False(engine.IsTemplateCached("template"));
			Assert.Equal("first", await engine.CompileRenderStringAsync("template", "first", new object()));
			Assert.True(engine.IsTemplateCached("template"));

			engine.InvalidateTemplate("template");

			Assert.False(engine.IsTemplateCached("template"));
			Assert.Equal("second", await engine.CompileRenderStringAsync("template", "second", new object()));
		}

		[Fact]
		public async Task Custom_Cache_Provider_Is_Used_End_To_End()
		{
			var provider = new TrackingCachingProvider();
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.UseCachingProvider(provider)
				.Build();

			Assert.Equal("cached", await engine.CompileRenderStringAsync("template", "cached", new object()));
			Assert.Equal("cached", await engine.CompileRenderStringAsync("template", "cached", new object()));

			Assert.True(provider.StoreCount > 0);
			Assert.True(provider.HitCount > 0);
		}

		[Fact]
		public void Cache_Facade_Is_Safe_When_Caching_Is_Disabled()
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();

			Assert.False(engine.IsTemplateCached("template"));
			engine.InvalidateTemplate("template");
			Assert.False(engine.IsTemplateCached("template"));
		}

		[Fact]
		public async Task Cache_Facade_Inspects_And_Invalidates_Embedded_Templates()
		{
			var engine = new RazorLightEngineBuilder()
				.UseEmbeddedResourcesProject(
					typeof(CacheCoherenceTest).Assembly,
					"RazorLight.Tests.Assets.Embedded")
				.UseMemoryCachingProvider()
				.Build();

			await engine.CompileRenderAsync("Empty.cshtml", new object());
			Assert.True(engine.IsTemplateCached("Empty.cshtml"));

			engine.InvalidateTemplate("Empty.cshtml");
			Assert.False(engine.IsTemplateCached("Empty.cshtml"));
		}

		[Fact]
		public async Task Remove_Invalidates_Page_And_Compilation_Caches()
		{
			var project = new MutableRazorProject();
			project.Set("template", "first", expirePrevious: false);
			var engine = CreateEngine(project);

			Assert.Equal("first", await engine.CompileRenderAsync("template", new object()));

			project.Set("template", "second", expirePrevious: false);
			Assert.Equal("first", await engine.CompileRenderAsync("template", new object()));

			engine.InvalidateTemplate("template");

			Assert.Equal("second", await engine.CompileRenderAsync("template", new object()));
		}

		[Fact]
		public async Task Failed_Compilation_Can_Be_Retried_With_The_Same_Key()
		{
			var project = new MutableRazorProject();
			project.Set("template", "@{ var value = ; }", expirePrevious: false);
			var engine = CreateEngine(project);

			await Assert.ThrowsAsync<TemplateCompilationException>(
				() => engine.CompileRenderAsync("template", new object()));

			project.Set("template", "recovered", expirePrevious: false);

			Assert.Equal("recovered", await engine.CompileRenderAsync("template", new object()));
		}

		[Fact]
		public async Task Project_Change_Tokens_Invalidate_Direct_Templates()
		{
			var project = new MutableRazorProject();
			project.Set("template", "direct-v1", expirePrevious: false);
			var engine = CreateEngine(project);

			Assert.Equal("direct-v1", await engine.CompileRenderAsync("template", new object()));

			project.Set("template", "direct-v2");

			Assert.Equal("direct-v2", await engine.CompileRenderAsync("template", new object()));
		}

		[Fact]
		public async Task Project_Change_Tokens_Invalidate_Includes()
		{
			var project = new MutableRazorProject();
			project.Set("parent", "start-@{ await IncludeAsync(\"include\", Model); }-end", expirePrevious: false);
			project.Set("include", "include-v1", expirePrevious: false);
			var engine = CreateEngine(project);

			Assert.Equal("start-include-v1-end", await engine.CompileRenderAsync("parent", new object()));

			project.Set("include", "include-v2");

			Assert.Equal("start-include-v2-end", await engine.CompileRenderAsync("parent", new object()));
		}

		[Fact]
		public async Task Project_Change_Tokens_Invalidate_Layouts()
		{
			var project = new MutableRazorProject();
			project.Set("page", "@{ Layout = \"layout\"; }body", expirePrevious: false);
			project.Set("layout", "layout-v1:@RenderBody()", expirePrevious: false);
			var engine = CreateEngine(project);

			Assert.Equal("layout-v1:body", await engine.CompileRenderAsync("page", new object()));

			project.Set("layout", "layout-v2:@RenderBody()");

			Assert.Equal("layout-v2:body", await engine.CompileRenderAsync("page", new object()));
		}

		[Fact]
		public async Task File_Project_Change_Tokens_Invalidate_Direct_Templates_Layouts_And_Includes()
		{
			string root = Path.Combine(Path.GetTempPath(), "RazorLight.Tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				string directPath = Path.Combine(root, "direct.cshtml");
				string includePath = Path.Combine(root, "include.cshtml");
				string layoutPath = Path.Combine(root, "layout.cshtml");
				File.WriteAllText(directPath, "direct-v1");
				File.WriteAllText(includePath, "include-v1");
				File.WriteAllText(Path.Combine(root, "parent.cshtml"), "start-@{ await IncludeAsync(\"include.cshtml\", Model); }-end");
				File.WriteAllText(layoutPath, "layout-v1:@RenderBody()");
				File.WriteAllText(Path.Combine(root, "page.cshtml"), "@{ Layout = \"layout.cshtml\"; }body");

				var engine = new RazorLightEngineBuilder()
					.UseFileSystemProject(root)
					.UseMemoryCachingProvider()
					.SetOperatingAssembly(typeof(CacheCoherenceTest).Assembly)
					.Build();

				Assert.Equal("direct-v1", await engine.CompileRenderAsync("direct.cshtml", new object()));
				Assert.Equal("start-include-v1-end", await engine.CompileRenderAsync("parent.cshtml", new object()));
				Assert.Equal("layout-v1:body", await engine.CompileRenderAsync("page.cshtml", new object()));
				Assert.True(engine.IsTemplateCached("direct.cshtml"));
				Assert.True(engine.IsTemplateCached("include.cshtml"));
				Assert.True(engine.IsTemplateCached("layout.cshtml"));

				File.WriteAllText(directPath, "direct-v2");
				File.WriteAllText(includePath, "include-v2");
				File.WriteAllText(layoutPath, "layout-v2:@RenderBody()");

				await AssertEventuallyAsync(async () =>
					await engine.CompileRenderAsync("direct.cshtml", new object()) == "direct-v2" &&
					await engine.CompileRenderAsync("parent.cshtml", new object()) == "start-include-v2-end" &&
					await engine.CompileRenderAsync("page.cshtml", new object()) == "layout-v2:body");
			}
			finally
			{
				Directory.Delete(root, recursive: true);
			}
		}

		[Fact]
		public async Task Concurrent_Compilation_Of_One_Key_Uses_One_Project_Read()
		{
			var project = new MutableRazorProject();
			project.Set("template", "concurrent", expirePrevious: false);
			var engine = CreateEngine(project);

			var renders = Enumerable.Range(0, 16)
				.Select(_ => engine.CompileRenderAsync("template", new object()));
			var results = await Task.WhenAll(renders);

			Assert.All(results, result => Assert.Equal("concurrent", result));
			Assert.Equal(1, project.ReadCount);
		}

		[Fact]
		public void Stale_InFlight_Compilation_Cannot_Repopulate_A_Removed_Key()
		{
			var compilerCache = new RecordingCompilerCache();
			var pageCache = new MemoryCachingProvider();
			var cache = new CoordinatedCachingProvider(pageCache, compilerCache);
			long versionBeforeRemoval = cache.GetVersion("template");

			cache.Remove("template");
			cache.StoreCompiledTemplate("template", "template.__razorlight.old", () => new TestPage(), null, versionBeforeRemoval);

			Assert.False(cache.Contains("template.__razorlight.old"));
			Assert.Contains("template", compilerCache.RemovedKeys);
		}

		[Fact]
		public async Task Concurrent_Retrieve_Replace_And_Remove_Leaves_A_Deterministic_Final_Value()
		{
			var cache = new CoordinatedCachingProvider(new MemoryCachingProvider(), new RecordingCompilerCache());

			var operations = Enumerable.Range(0, 100).Select(index => Task.Run(() =>
			{
				if (index % 3 == 0)
				{
					cache.Remove("template");
				}
				else
				{
					cache.CacheTemplate("template", () => new TestPage(), null);
					cache.TryGetTemplate("template", out _);
				}
			}));

			await Task.WhenAll(operations);

			var finalPage = new TestPage();
			cache.CacheTemplate("template", () => finalPage, null);
			Assert.True(cache.TryGetTemplate("template", out var finalFactory));
			Assert.Same(finalPage, finalFactory());
		}

		[Fact]
		public void Cache_Keys_Normalize_Separators_But_Remain_Case_Sensitive()
		{
			var cache = new CoordinatedCachingProvider(new MemoryCachingProvider(), new SlashNormalizingCompilerCache());
			var page = new TestPage();

			cache.CacheTemplate("folder\\template.cshtml", () => page, null);

			Assert.True(cache.TryGetTemplate("folder/template.cshtml", out var pageFactory));
			Assert.Same(page, pageFactory());
			Assert.False(cache.Contains("folder/Template.cshtml"));
		}

		private static IRazorLightEngine CreateEngine(RazorLightProject project)
		{
			return new RazorLightEngineBuilder()
				.UseProject(project)
				.UseMemoryCachingProvider()
				.SetOperatingAssembly(typeof(CacheCoherenceTest).Assembly)
				.Build();
		}

		private sealed class TrackingCachingProvider : ICachingProvider
		{
			private readonly MemoryCachingProvider _inner = new MemoryCachingProvider();

			public int HitCount { get; private set; }
			public int StoreCount { get; private set; }

			public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
			{
				bool found = _inner.TryGetTemplate(key, out pageFactory);
				if (found)
				{
					HitCount++;
				}

				return found;
			}

			public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken)
			{
				StoreCount++;
				_inner.CacheTemplate(key, pageFactory, expirationToken);
			}

			public bool Contains(string key) => _inner.Contains(key);

			public void Remove(string key) => _inner.Remove(key);
		}

		private static async Task AssertEventuallyAsync(Func<Task<bool>> condition)
		{
			var timeout = Stopwatch.StartNew();
			while (timeout.Elapsed < TimeSpan.FromSeconds(10))
			{
				if (await condition())
				{
					return;
				}

				await Task.Delay(50);
			}

			Assert.Fail("File change tokens did not invalidate every dependent template within 10 seconds.");
		}

		private sealed class MutableRazorProject : RazorLightProject
		{
			private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.Ordinal);
			private int _readCount;

			public int ReadCount => Volatile.Read(ref _readCount);

			public void Set(string key, string content, bool expirePrevious = true)
			{
				var replacement = new Entry(content);
				if (_items.TryGetValue(key, out var previous))
				{
					_items[key] = replacement;
					if (expirePrevious)
					{
						previous.Expiration.Cancel();
					}
				}
				else
				{
					_items[key] = replacement;
				}
			}

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			{
				Interlocked.Increment(ref _readCount);
				if (!_items.TryGetValue(templateKey, out var entry))
				{
					return Task.FromResult<RazorLightProjectItem>(NoRazorProjectItem.Empty);
				}

				return Task.FromResult<RazorLightProjectItem>(new TextSourceRazorProjectItem(templateKey, entry.Content)
				{
					ExpirationToken = new CancellationChangeToken(entry.Expiration.Token),
				});
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			{
				return Task.FromResult<IEnumerable<RazorLightProjectItem>>(Array.Empty<RazorLightProjectItem>());
			}

			private sealed record Entry(string Content)
			{
				public CancellationTokenSource Expiration { get; } = new();
			}
		}

		private class RecordingCompilerCache : ITemplateCompilerCache
		{
			public ConcurrentBag<string> RemovedKeys { get; } = new();

			public virtual string NormalizeKey(string key) => key;

			public void Remove(string key) => RemovedKeys.Add(key);
		}

		private sealed class SlashNormalizingCompilerCache : RecordingCompilerCache
		{
			public override string NormalizeKey(string key) => key.Replace('\\', '/');
		}

		private sealed class TestPage : TemplatePage
		{
			public override Task ExecuteAsync() => Task.CompletedTask;

			public override void SetModel(object? model)
			{
			}
		}
	}
}
