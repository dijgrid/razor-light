using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RazorLight.Razor;
using RazorLight.Tests.Integration;
using Xunit;

namespace RazorLight.Tests
{
	[Collection(NonParallelRazorCompilationCollection.Name)]
	public class CancellationTest
	{
		[Fact]
		public async Task Cancellation_Before_Start_Does_Not_Populate_Cache()
		{
			var engine = new RazorLightEngineBuilder().UseNoProject().UseMemoryCachingProvider().Build();
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				engine.CompileRenderStringAsync("cancelled", "content", new object(), cancellationSource.Token));

			Assert.False(engine.IsTemplateCached("cancelled"));
		}

		[Fact]
		public async Task Cancellation_During_Project_Lookup_Does_Not_Poison_Retry()
		{
			var project = new CancellableProject("completed");
			var engine = new RazorLightEngineBuilder()
				.UseProject(project)
				.UseMemoryCachingProvider()
				.Build();
			using var cancellationSource = new CancellationTokenSource();

			Task<ITemplatePage> cancelled = engine.CompileTemplateAsync("template", cancellationSource.Token);
			await project.LookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
			project.ReleaseLookup();

			Assert.Equal("completed", await engine.CompileRenderAsync("template", new object()));
			Assert.True(engine.IsTemplateCached("template"));
		}

		[Fact]
		public async Task Cancellation_Propagates_To_Import_Lookup()
		{
			var project = new CancellableImportProject();
			var engine = new RazorLightEngineBuilder().UseProject(project).Build();
			using var cancellationSource = new CancellationTokenSource();

			Task<ITemplatePage> cancelled = engine.CompileTemplateAsync("template", cancellationSource.Token);
			await project.ImportLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
			Assert.False(project.ObservedToken.CanBeCanceled);
		}

		[Fact]
		public async Task Render_Cancellation_Is_Available_To_Template_Code()
		{
			var templates = new Dictionary<string, string>
			{
				["wait"] = "@{ await Task.Delay(-1, CancellationToken); }",
			};
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDynamicTemplates(templates)
				.Build();
			ITemplatePage page = await engine.CompileTemplateAsync("wait");
			using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				engine.RenderTemplateAsync(page, new object(), cancellationSource.Token));
		}

		[Fact]
		public async Task Include_Uses_The_Parent_Render_Cancellation()
		{
			var templates = new Dictionary<string, string>
			{
				["parent"] = "@{ await IncludeAsync(\"child\"); }",
				["child"] = "@{ await Task.Delay(-1, CancellationToken); }",
			};
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.AddDynamicTemplates(templates)
				.Build();
			ITemplatePage page = await engine.CompileTemplateAsync("parent");
			using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				engine.RenderTemplateAsync(page, new object(), cancellationSource.Token));
		}

		[Fact]
		public async Task Render_Waits_For_Token_Ignoring_Page_Before_Returning_Cancellation()
		{
			var started = NewCompletionSource();
			var release = NewCompletionSource();
			var page = new TestPage(async page =>
			{
				started.TrySetResult();
				await release.Task;
				page.WriteLiteral("completed");
			});
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();
			using var cancellationSource = new CancellationTokenSource();

			Task<string> render = engine.RenderTemplateAsync(page, new object(), cancellationSource.Token);
			await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			Assert.False(render.IsCompleted);
			release.TrySetResult();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => render);
		}

		[Fact]
		public async Task Explicit_Include_Token_Reaches_Include_Operation()
		{
			CancellationToken observedToken = default;
			var page = new TestPage(_ => Task.CompletedTask)
			{
				PageContext = new PageContext { CancellationToken = new CancellationTokenSource().Token },
				IncludeFunc = (_, _, cancellationToken) =>
				{
					observedToken = cancellationToken;
					return Task.CompletedTask;
				},
			};
			using var explicitSource = new CancellationTokenSource();

			await page.IncludeAsync("child", null, explicitSource.Token);

			Assert.Equal(explicitSource.Token, observedToken);
		}

		[Fact]
		public async Task Section_Cancellation_Waits_For_Token_Ignoring_Delegate()
		{
			var started = NewCompletionSource();
			var release = NewCompletionSource();
			var page = new TestPage(_ => Task.CompletedTask)
			{
				PreviousSectionWriters = new Dictionary<string, RenderAsyncDelegate>
				{
					["delayed"] = async () =>
					{
						started.TrySetResult();
						await release.Task;
					},
				},
			};
			using var cancellationSource = new CancellationTokenSource();

			Task section = page.RenderSectionAsync("delayed", cancellationSource.Token);
			await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			Assert.False(section.IsCompleted);
			release.TrySetResult();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => section);
		}

		[Fact]
		public async Task Final_Writer_Receives_Render_Cancellation_Token()
		{
			var page = new TestPage(page =>
			{
				page.WriteLiteral("content");
				return Task.CompletedTask;
			});
			var writer = new CancellableWriter();
			var engine = new RazorLightEngineBuilder().UseNoProject().Build();
			using var cancellationSource = new CancellationTokenSource();

			Task render = engine.RenderTemplateAsync(
				page,
				new object(),
				writer,
				cancellationSource.Token);
			await writer.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => render);
			Assert.Equal(cancellationSource.Token, writer.ObservedToken);
		}

		[Fact]
		public async Task Parameterless_Flush_Uses_Active_Page_Token()
		{
			using var cancellationSource = new CancellationTokenSource();
			var writer = new FlushObservingWriter();
			var page = new TestPage(_ => Task.CompletedTask)
			{
				PageContext = new PageContext
				{
					CancellationToken = cancellationSource.Token,
					Writer = writer,
				},
			};

			await page.FlushAsync();

			Assert.Equal(cancellationSource.Token, writer.ObservedToken);
		}

		[Fact]
		public async Task Cancellation_After_Cache_Population_Preserves_The_Cached_Template()
		{
			var engine = new RazorLightEngineBuilder()
				.UseNoProject()
				.UseMemoryCachingProvider()
				.Build();

			Assert.Equal("cached", await engine.CompileRenderStringAsync("cached", "cached", new object()));
			using var cancellationSource = new CancellationTokenSource();
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
				engine.CompileRenderStringAsync("cached", "cached", new object(), cancellationSource.Token));

			Assert.True(engine.IsTemplateCached("cached"));
			Assert.Equal("cached", await engine.CompileRenderStringAsync("cached", "cached", new object()));
		}

		private sealed class CancellableProject : RazorLightProject
		{
			private readonly string _content;
			private readonly TaskCompletionSource _release =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			public CancellableProject(string content)
			{
				_content = content;
			}

			public TaskCompletionSource LookupStarted { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) =>
				GetItemAsync(templateKey, CancellationToken.None);

			public override async Task<RazorLightProjectItem> GetItemAsync(
				string templateKey,
				CancellationToken cancellationToken)
			{
				LookupStarted.TrySetResult();
				await _release.Task.WaitAsync(cancellationToken);
				return new TextSourceRazorProjectItem(templateKey, _content);
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
				Task.FromResult<IEnumerable<RazorLightProjectItem>>(Array.Empty<RazorLightProjectItem>());

			public void ReleaseLookup() => _release.TrySetResult();
		}

		private sealed class CancellableImportProject : RazorLightProject
		{
			public TaskCompletionSource ImportLookupStarted { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			public CancellationToken ObservedToken { get; private set; }

			public override Task<RazorLightProjectItem> GetItemAsync(string templateKey) =>
				Task.FromResult<RazorLightProjectItem>(new ImportableProjectItem(templateKey, "content"));

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
				GetImportsAsync(templateKey, CancellationToken.None);

			public override async Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(
				string templateKey,
				CancellationToken cancellationToken)
			{
				ObservedToken = cancellationToken;
				ImportLookupStarted.TrySetResult();
				await Task.Delay(-1, cancellationToken);
				return Array.Empty<RazorLightProjectItem>();
			}
		}

		private sealed class ImportableProjectItem : RazorLightProjectItem
		{
			private readonly byte[] _content;

			public ImportableProjectItem(string key, string content)
			{
				Key = key;
				_content = Encoding.UTF8.GetBytes(content);
			}

			public override string Key { get; }

			public override bool Exists => true;

			public override Stream Read() => new MemoryStream(_content, writable: false);
		}

		private static TaskCompletionSource NewCompletionSource() =>
			new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		private sealed class TestPage : TemplatePage
		{
			private readonly Func<TestPage, Task> _execute;

			public TestPage(Func<TestPage, Task> execute)
			{
				_execute = execute;
				Key = "test";
			}

			public override Task ExecuteAsync() => _execute(this);
			public override void SetModel(object? model) { }
			public override void BeginContext(int position, int length, bool isLiteral) { }
			public override void EndContext() { }
		}

		private sealed class CancellableWriter : StringWriter
		{
			public TaskCompletionSource WriteStarted { get; } = NewCompletionSource();
			public CancellationToken ObservedToken { get; private set; }

			public override async Task WriteAsync(
				ReadOnlyMemory<char> buffer,
				CancellationToken cancellationToken = default)
			{
				ObservedToken = cancellationToken;
				WriteStarted.TrySetResult();
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			}
		}

		private sealed class FlushObservingWriter : StringWriter
		{
			public CancellationToken ObservedToken { get; private set; }

			public override Task FlushAsync(CancellationToken cancellationToken)
			{
				ObservedToken = cancellationToken;
				return Task.CompletedTask;
			}
		}
	}
}
