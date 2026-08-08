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
			Assert.Equal(cancellationSource.Token, project.ObservedToken);
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
	}
}
