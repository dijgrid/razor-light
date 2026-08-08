using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using RazorLight.Caching;
using RazorLight.Compilation;
using RazorLight.Extensions;
using RazorLight.Razor;
using RazorLight.Tests.Integration;
using Xunit;

namespace RazorLight.Tests
{
	[Collection(NonParallelRazorCompilationCollection.Name)]
	public class RenderLifecycleTest
	{
		private const string InjectionDirective =
			"@inject RazorLight.Tests.RenderLifecycleTest.ScopedProbe Probe\n";

		[Fact]
		public async Task Dependency_Injection_Created_Engine_Injects_String_Templates()
		{
			var probes = new ConcurrentBag<ScopedProbe>();
			using var provider = CreateServices(probes).BuildServiceProvider(validateScopes: true);
			var engine = provider.GetRequiredService<IRazorLightEngine>();

			string first = await engine.CompileRenderStringAsync(
				"injected-string",
				InjectionDirective + "string:@Probe.Id",
				new object());
			string second = await engine.CompileRenderStringAsync(
				"injected-string",
				InjectionDirective + "string:@Probe.Id",
				new object());

			Assert.StartsWith("string:", first.Trim());
			Assert.StartsWith("string:", second.Trim());
			Assert.NotEqual(first, second);
			Assert.Equal(2, probes.Count);
			Assert.All(probes, probe => Assert.True(probe.IsDisposed));
		}

		[Fact]
		public async Task Dependency_Injection_Created_Engine_Injects_File_Templates()
		{
			string root = Path.Combine(Path.GetTempPath(), "RazorLight.Tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				File.WriteAllText(Path.Combine(root, "injected.cshtml"), InjectionDirective + "file:@Probe.Id");
				var probes = new ConcurrentBag<ScopedProbe>();
				var services = CreateServices(probes, configureEngine: false);
				services.AddRazorLight()
					.SetOperatingAssembly(typeof(RenderLifecycleTest).Assembly)
					.UseFileSystemProject(root);
				using var provider = services.BuildServiceProvider(validateScopes: true);

				string result = await provider.GetRequiredService<IRazorLightEngine>()
					.CompileRenderAsync("injected.cshtml", new object());

				Assert.StartsWith("file:", result.Trim());
				Assert.Single(probes);
				Assert.True(probes.Single().IsDisposed);
			}
			finally
			{
				Directory.Delete(root, recursive: true);
			}
		}

		[Fact]
		public async Task Dependency_Injection_Created_Engine_Injects_Embedded_Templates()
		{
			var probes = new ConcurrentBag<ScopedProbe>();
			var services = CreateServices(probes, configureEngine: false);
			services.AddRazorLight()
				.SetOperatingAssembly(typeof(RenderLifecycleTest).Assembly)
				.UseEmbeddedResourcesProject(typeof(Root));
			using var provider = services.BuildServiceProvider(validateScopes: true);

			string result = await provider.GetRequiredService<IRazorLightEngine>()
				.CompileRenderAsync("Assets.Embedded.Injected", new object());

			Assert.StartsWith("embedded:", result.Trim());
			Assert.Single(probes);
			Assert.True(probes.Single().IsDisposed);
		}

		[Fact]
		public async Task One_Render_Scope_Is_Shared_By_Custom_Page_Layout_And_Include()
		{
			var project = new DictionaryProject(new Dictionary<string, string>
			{
				["page"] = InjectionDirective + "@{ Layout = \"layout\"; }page:@Probe.Id|@{ await IncludeAsync(\"include\", Model); }",
				["include"] = InjectionDirective + "include:@Probe.Id",
				["layout"] = InjectionDirective + "layout:@Probe.Id|@RenderBody()",
			});
			var probes = new ConcurrentBag<ScopedProbe>();
			var initializedKeys = new ConcurrentBag<string>();
			var services = CreateServices(probes, configureEngine: false);
			services.AddRazorLight()
				.SetOperatingAssembly(typeof(RenderLifecycleTest).Assembly)
				.AddPageInitializer(page => initializedKeys.Add(
					page.Key ?? throw new InvalidOperationException("An initialized page must have a key.")));
			services.RemoveAll<RazorLightProject>();
			services.AddSingleton<RazorLightProject>(project);
			using var provider = services.BuildServiceProvider(validateScopes: true);

			string result = await provider.GetRequiredService<IRazorLightEngine>()
				.CompileRenderAsync("page", new object());

			string[] ids = result.Split('|')
				.Select(part => part.Substring(part.IndexOf(':') + 1).Trim())
				.ToArray();
			Assert.Equal(3, ids.Length);
			Assert.Single(ids.Distinct(StringComparer.Ordinal));
			Assert.Single(probes);
			Assert.True(probes.Single().IsDisposed);
			Assert.Equal(3, initializedKeys.Count);
			Assert.Equal(3, initializedKeys.Distinct(StringComparer.Ordinal).Count());
		}

		[Fact]
		public async Task Builder_Created_Engine_Does_Not_Imply_Service_Injection()
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();

			string result = await engine.CompileRenderStringAsync(
				"builder-inject",
				InjectionDirective + "@(Probe == null ? \"not-injected\" : \"injected\")",
				new object());

			Assert.Equal("not-injected", result.Trim());
		}

		[Fact]
		public async Task Builder_Page_Initializer_Runs_Exactly_Once_Per_Page()
		{
			var project = new DictionaryProject(new Dictionary<string, string>
			{
				["page"] = "@{ Layout = \"layout\"; }page|@{ await IncludeAsync(\"include\", Model); }",
				["include"] = "include",
				["layout"] = "layout|@RenderBody()",
			});
			var initializedKeys = new ConcurrentBag<string>();
			var engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.AddPageInitializer(page => initializedKeys.Add(
					page.Key ?? throw new InvalidOperationException("An initialized page must have a key.")))
				.Build();

			await engine.CompileRenderAsync("page", new object());

			Assert.Equal(3, initializedKeys.Count);
			Assert.Equal(3, initializedKeys.Distinct(StringComparer.Ordinal).Count());
		}

		[Fact]
		public async Task Cancellation_Does_Not_Dispose_Render_Scope_While_Page_Is_Still_Running()
		{
			var probes = new ConcurrentBag<ScopedProbe>();
			using var provider = CreateServices(probes).BuildServiceProvider(validateScopes: true);
			var page = new ScopedCancellationPage();
			using var cancellationSource = new CancellationTokenSource();

			Task render = provider.GetRequiredService<IRazorLightEngine>()
				.RenderTemplateAsync(page, new object(), cancellationSource.Token);
			await page.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			Assert.False(render.IsCompleted);
			page.Release.TrySetResult();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => render);
			Assert.False(page.ProbeWasDisposedDuringExecution);
			Assert.Single(probes);
			Assert.True(probes.Single().IsDisposed);
		}

		[Fact]
		public async Task Null_Model_Replaces_Preexisting_Page_Model()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDynamicTemplates(new Dictionary<string, string>
				{
					["nullable-model"] = "@model string\n@(Model ?? \"null\")",
				})
				.Build();
			ITemplatePage page = await engine.CompileTemplateAsync("nullable-model");
			page.SetModel("stale");

			string result = await engine.RenderTemplateAsync<string?>(page, null);

			Assert.Equal("null", result.Trim());
		}

		[Fact]
		public async Task Page_Instances_Are_Single_Use()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDynamicTemplates(new Dictionary<string, string>
				{
					["single-use"] = "@{ Layout = \"single-use-layout\"; }\n@section value {\ncontent\n}",
					["single-use-layout"] = "@RenderSection(\"value\")",
				})
				.Build();
			ITemplatePage page = await engine.CompileTemplateAsync("single-use");

			Assert.Equal("content", (await engine.RenderTemplateAsync(page, new object())).Trim());
			InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				engine.RenderTemplateAsync(page, new object()));
			Assert.Contains("single-use", exception.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public async Task Concurrent_Page_Reuse_Is_Rejected_While_First_Render_Completes()
		{
			var page = new BlockingPage();
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();
			Task<string> first = engine.RenderTemplateAsync(page, new object());
			await page.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				engine.RenderTemplateAsync(page, new object()));
			page.Release.TrySetResult();

			Assert.Equal("complete", await first);
		}

		[Fact]
		public async Task Reusable_Template_Creates_Fresh_Pages_For_Concurrent_Renders()
		{
			await using var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDynamicTemplates(new Dictionary<string, string> { ["reusable"] = "@Model" })
				.UseMemoryCachingProvider()
				.Build();
			RazorLightTemplate template = await engine.CompileReusableTemplateAsync("reusable");

			string[] results = await Task.WhenAll(
				template.RenderAsync("first"),
				template.RenderAsync("second"));

			Assert.Equal(new[] { "first", "second" }, results);
		}

		[Fact]
		public void Builder_Disposes_Owned_Compiler_Cache_Project_And_Caching_Provider()
		{
			string root = Path.Combine(Path.GetTempPath(), "RazorLight.Tests", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(root);
			try
			{
				var engine = (RazorLightEngine)new RazorLightEngineBuilder()
					.UseFileSystemProject(root)
					.UseMemoryCachingProvider()
					.Build();
				var handler = (EngineHandler)engine.Handler;
				var compiler = (RazorTemplateCompiler)handler.Compiler;
				var project = (FileSystemRazorProject)compiler.Project;
				var cache = (MemoryCachingProvider)handler.OwnedCachingProvider!;

				engine.Dispose();
				engine.Dispose();

				Assert.True(compiler.IsDisposed);
				Assert.True(project.IsDisposed);
				Assert.True(cache.IsDisposed);
			}
			finally
			{
				Directory.Delete(root, recursive: true);
			}
		}

		[Fact]
		public void Builder_Does_Not_Dispose_Caller_Owned_Project_Or_Cache()
		{
			var project = new DisposableProject();
			var cache = new DisposableCache();
			IRazorLightEngine engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.UseCachingProvider(cache)
				.Build();

			engine.Dispose();

			Assert.False(project.IsDisposed);
			Assert.False(cache.IsDisposed);
		}

		[Fact]
		public void Repeated_Engine_Creation_And_Disposal_Is_Safe()
		{
			for (int index = 0; index < 20; index++)
			{
				using IRazorLightEngine engine = new RazorLightEngineBuilder()
					.UseNoProject()
					.UseMemoryCachingProvider()
					.Build();
			}
		}

		[Fact]
		public void Dependency_Injection_Disposes_The_Singleton_Engine_Compiler()
		{
			var probes = new ConcurrentBag<ScopedProbe>();
			ServiceProvider provider = CreateServices(probes).BuildServiceProvider(validateScopes: true);
			var engine = (RazorLightEngine)provider.GetRequiredService<IRazorLightEngine>();
			var compiler = (RazorTemplateCompiler)engine.Handler.Compiler;

			provider.Dispose();

			Assert.True(compiler.IsDisposed);
		}

		[Fact]
		public async Task Dependency_Injection_Options_Are_Snapshotted_Before_Runtime_Services()
		{
			var probes = new ConcurrentBag<ScopedProbe>();
			var services = CreateServices(probes);
			services.Configure<RazorLightOptions>(options =>
				options.Namespaces.Add("RazorLight.Tests"));
			using var provider = services.BuildServiceProvider(validateScopes: true);
			var engine = provider.GetRequiredService<IRazorLightEngine>();
			provider.GetRequiredService<IOptions<RazorLightOptions>>().Value.Namespaces.Clear();

			string result = await engine.CompileRenderStringAsync(
				"di-options-snapshot",
				"@model RenderLifecycleTest.ScopedProbe\n@Model.Id",
				new ScopedProbe());

			Assert.NotEmpty(result.Trim());
		}

		[Fact]
		public async Task Missing_ViewBag_Members_Return_Null_And_Null_Conditional_Access_Works()
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();
			dynamic viewBag = new ExpandoObject();
			viewBag.Parent = new ExpandoObject();
			viewBag.Parent.Name = "nested";

			string result = await engine.CompileRenderStringAsync(
				"viewbag-missing",
				"@(ViewBag.Missing ?? \"fallback\")|@(ViewBag.Absent?.Child ?? \"safe\")|@ViewBag.Parent.Name",
				new object(),
				viewBag);

			Assert.Equal("fallback|safe|nested", result);
		}

		[Theory]
		[InlineData("@ViewBag.Missing()")]
		[InlineData("@ViewBag[\"Missing\"]")]
		[InlineData("@{ int value = ViewBag.Text; }", "text")]
		public async Task Invalid_ViewBag_Operations_Retain_Dynamic_Binding_Errors(string template, string? text = null)
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();
			dynamic viewBag = new ExpandoObject();
			viewBag.Text = text;

			await Assert.ThrowsAsync<RuntimeBinderException>(() => engine.CompileRenderStringAsync(
				Guid.NewGuid().ToString("N"),
				template,
				new object(),
				viewBag));
		}

		private static ServiceCollection CreateServices(
			ConcurrentBag<ScopedProbe> probes,
			bool configureEngine = true)
		{
			var services = new ServiceCollection();
			services.AddScoped(_ =>
			{
				var probe = new ScopedProbe();
				probes.Add(probe);
				return probe;
			});
			if (configureEngine)
			{
				services.AddRazorLight().SetOperatingAssembly(typeof(RenderLifecycleTest).Assembly);
			}
			return services;
		}

		public sealed class ScopedProbe : IDisposable
		{
			public string Id { get; } = Guid.NewGuid().ToString("N");
			public bool IsDisposed { get; private set; }
			public void Dispose() => IsDisposed = true;
		}

		private sealed class ScopedCancellationPage : TemplatePage
		{
			public TaskCompletionSource Started { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			public TaskCompletionSource Release { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			[RazorInject]
			public ScopedProbe Probe { get; set; } = null!;

			public bool ProbeWasDisposedDuringExecution { get; private set; }

			public override async Task ExecuteAsync()
			{
				Started.TrySetResult();
				await Release.Task;
				ProbeWasDisposedDuringExecution = Probe.IsDisposed;
			}

			public override void SetModel(object? model) { }
			public override void BeginContext(int position, int length, bool isLiteral) { }
			public override void EndContext() { }
		}

		private sealed class BlockingPage : TemplatePage
		{
			public TaskCompletionSource Started { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			public TaskCompletionSource Release { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			public override async Task ExecuteAsync()
			{
				Started.TrySetResult();
				await Release.Task;
				WriteLiteral("complete");
			}

			public override void SetModel(object? model) { }
			public override void BeginContext(int position, int length, bool isLiteral) { }
			public override void EndContext() { }
		}

		private sealed class DisposableProject : RazorLightProject, IDisposable
		{
			public bool IsDisposed { get; private set; }
			public void Dispose() => IsDisposed = true;
			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) =>
				Task.FromResult<RazorLightProjectItem>(NoRazorProjectItem.Empty);
			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
				Task.FromResult<IEnumerable<RazorLightProjectItem>>(Array.Empty<RazorLightProjectItem>());
		}

		private sealed class DisposableCache : ICachingProvider, IDisposable
		{
			public bool IsDisposed { get; private set; }
			public bool Contains(string key) => false;
			public void CacheTemplate(string key, Func<ITemplatePage> pageFactory, IChangeToken? expirationToken) { }
			public void Dispose() => IsDisposed = true;
			public void Remove(string key) { }
			public bool TryGetTemplate(string key, [NotNullWhen(true)] out Func<ITemplatePage>? pageFactory)
			{
				pageFactory = null;
				return false;
			}
		}

		private sealed class DictionaryProject : RazorLightProject
		{
			private readonly IReadOnlyDictionary<string, string> _templates;

			public DictionaryProject(IReadOnlyDictionary<string, string> templates)
			{
				_templates = templates;
			}

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			{
				return Task.FromResult<RazorLightProjectItem>(_templates.TryGetValue(templateKey, out string? content)
					? new TextSourceRazorProjectItem(templateKey, content)
					: NoRazorProjectItem.Empty);
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
			{
				return Task.FromResult<IEnumerable<RazorLightProjectItem>>(Array.Empty<RazorLightProjectItem>());
			}
		}
	}
}
