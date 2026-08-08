using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using RazorLight.Compatibility;
using RazorLight.Compilation;
using RazorLight.Generation;
using RazorLight.Razor;
using RazorLight.Tests.Razor;
using Xunit;

namespace RazorLight.Tests.Compilation
{
	public class RazorTemplateCompilerTest
	{
		[Fact]
		public async Task Cancelling_One_Cache_Waiter_Does_Not_Cancel_Shared_Compilation()
		{
			var compiler = TestRazorTemplateCompiler.Create();
			var sharedSource = new TaskCompletionSource<CompiledTemplateDescriptor>(TaskCreationOptions.RunContinuationsAsynchronously);
			_ = compiler.Cache.Set("shared", sharedSource.Task);
			using var cancellationSource = new CancellationTokenSource();

			Task<CompiledTemplateDescriptor> cancelledWaiter = compiler.CompileAsync("shared", cancellationSource.Token);
			Task<CompiledTemplateDescriptor> survivingWaiter = compiler.CompileAsync("shared");
			cancellationSource.Cancel();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWaiter);
			Assert.False(survivingWaiter.IsCompleted);

			var descriptor = new CompiledTemplateDescriptor { TemplateKey = "shared" };
			sharedSource.SetResult(descriptor);
			Assert.Same(descriptor, await survivingWaiter);
		}

		[Fact]
		public async Task Unrelated_Template_Compilation_Does_Not_Wait_For_Blocked_Key()
		{
			var project = new IndependentlyBlockingProject();
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			Task<CompiledTemplateDescriptor> blocked = compiler.CompileAsync("blocked");
			await project.BlockedLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

			CompiledTemplateDescriptor independent = await compiler
				.CompileAsync("independent")
				.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal("independent", independent.TemplateKey);
			project.ReleaseBlocked.TrySetResult();
			Assert.Equal("blocked", (await blocked).TemplateKey);
		}

		[Fact]
		public void Ensure_Throws_OnNull_Constructor_Dependencies()
		{
			var options = new RazorLightOptions();
			var metadataManager = new DefaultMetadataReferenceManager();
			var assembly = Assembly.GetCallingAssembly();
			var project = new EmbeddedRazorProject(assembly);
			var compilerService = new RoslynCompilationService(metadataManager, assembly);
			var generator = new RazorSourceGenerator(Razor6CompilerCompatibility.CreateEngine(), project);

			Action p1 = new Action(() => { new RazorTemplateCompiler(null!, compilerService, project, options); });
			Action p2 = new Action(() => { new RazorTemplateCompiler(generator, null!, project, options); });
			Action p3 = new Action(() => { new RazorTemplateCompiler(generator, compilerService, null!, options); });

			Action p4 = new Action(() => { new RazorTemplateCompiler(generator, compilerService, project, (RazorLightOptions)null!); });

			Assert.Throws<ArgumentNullException>(p1);
			Assert.Throws<ArgumentNullException>(p2);
			Assert.Throws<ArgumentNullException>(p3);
			Assert.Throws<ArgumentNullException>(p4);
		}

		[Fact]
		public void TemplateKey_NotNormalized_OnStringRendering()
		{
			string templateKey = "key";

			var options = new RazorLightOptions();
			options.DynamicTemplates.Add(templateKey, "Template content");
			var compiler = TestRazorTemplateCompiler.Create(options);

			string normalizedKey = compiler.GetNormalizedKey(templateKey);

			Assert.NotNull(normalizedKey);
			Assert.Equal(templateKey, normalizedKey);
		}

		[Fact]
		public void TemplateKey_Normalized_On_FilesystemProject()
		{
			string templateKey = "key";
			var project = new FileSystemRazorProject("/");
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			string normalizedKey = compiler.GetNormalizedKey(templateKey);

			Assert.NotNull(normalizedKey);
			Assert.Equal($"/{templateKey}", normalizedKey);
		}

		[Fact]
		public void TemplateKey_NotNormalized_On_NonFileSystemProject()
		{
			string templateKey = "key";
			var project = new EmbeddedRazorProject(typeof(Root).Assembly);
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			string normalizedKey = compiler.GetNormalizedKey(templateKey);

			Assert.NotNull(normalizedKey);
			Assert.Equal(templateKey, normalizedKey);
		}

		[Fact]
		public async Task Compiler_Takes_Result_From_Cache_OnCompileAsync()
		{
			string templateKey = "key";
			var descriptor = new CompiledTemplateDescriptor();
			var descriptorTask = Task.FromResult(descriptor);

			var compiler = TestRazorTemplateCompiler.Create();

			_ = compiler.Cache.Set(templateKey, descriptorTask);

			CompiledTemplateDescriptor result = await compiler.CompileAsync(templateKey);

			Assert.NotNull(result);
			Assert.Same(descriptor, result);
		}

		[Fact]
		public async Task Compiler_Searches_WithNormalizedKey_IfNotFound()
		{
			string templateKey = "key";
			var descriptor = new CompiledTemplateDescriptor();
			var descriptorTask = Task.FromResult(descriptor);

			var project = new FileSystemRazorProject("/");
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			string normalizedKey = compiler.GetNormalizedKey(templateKey);

			_ = compiler.Cache.Set(normalizedKey, descriptorTask);

			CompiledTemplateDescriptor result = await compiler.CompileAsync(templateKey);

			Assert.NotNull(result);
			Assert.Same(descriptor, result);
		}

		[Fact]
		public async Task Throws_TemplateNotFoundException_If_ProjectItem_NotExist()
		{
			var project = new EmbeddedRazorProject(typeof(Root).Assembly);
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			Func<Task> task = new Func<Task>(() => compiler.CompileAsync("Not.Existing.Key"));

			await Assert.ThrowsAsync<TemplateNotFoundException>(task);
		}

		[Fact]
		public async Task Failed_Compilations_Do_Not_Retain_InFlight_Or_Generation_State()
		{
			var compiler = TestRazorTemplateCompiler.Create(project: new TestRazorProject
			{
				Value = NoRazorProjectItem.Empty,
			});

			for (int index = 0; index < 20; index++)
			{
				await Assert.ThrowsAsync<TemplateNotFoundException>(() =>
					compiler.CompileAsync("missing-" + index));
			}

			Assert.Equal(0, compiler.ActiveCompilationCount);
			Assert.Equal(0, compiler.CacheGenerationCount);
		}

		[Fact]
		public async Task Ensure_TemplateNotFoundException_KnownKeys_NotNull_When_EnableDebugMode_True()
		{
			var options = new RazorLightOptions { EnableDebugMode = true };
			var project = new EmbeddedRazorProject(typeof(Root));
			var compiler = TestRazorTemplateCompiler.Create(options, project);
			var item = new EmbeddedRazorProjectItem(typeof(Root), "Any.Key");

			var exception = await compiler.CreateTemplateNotFoundException(item);

			Assert.NotNull(exception.KnownDynamicTemplateKeys);
			Assert.NotNull(exception.KnownProjectTemplateKeys);
		}

		[Fact]
		public async Task Ensure_TemplateNotFoundException_KnownKeys_Null_When_EnableDebugMode_False()
		{
			var options = new RazorLightOptions { EnableDebugMode = false };
			var project = new EmbeddedRazorProject(typeof(Root));
			var compiler = TestRazorTemplateCompiler.Create(options, project);
			var item = new EmbeddedRazorProjectItem(typeof(Root), "Any.Key");

			var exception = await compiler.CreateTemplateNotFoundException(item);

			Assert.Null(exception.KnownDynamicTemplateKeys);
			Assert.Null(exception.KnownProjectTemplateKeys);
		}

		[Fact]
		public async Task Ensure_TemplateNotFoundException_KnownDynamicTemplateKeys_Exist_When_EnableDebugMode_True()
		{
			var dynamicTemplateKeys = new[] { "dynamicKey1", "dynamicKey2" };

			var project = new EmbeddedRazorProject(typeof(Root).Assembly, "RazorLight.Tests.Assets.Embedded");
			var options = new RazorLightOptions { EnableDebugMode = true };
			foreach (var dynamicKey in dynamicTemplateKeys) options.DynamicTemplates.Add(dynamicKey, "Content");
			var compiler = TestRazorTemplateCompiler.Create(options, project);
			var item = new EmbeddedRazorProjectItem(typeof(Root), "Any.Key");

			var exception = await compiler.CreateTemplateNotFoundException(item);

			Assert.NotNull(exception.KnownDynamicTemplateKeys);
			Assert.Equal(dynamicTemplateKeys.OrderBy(x => x), exception.KnownDynamicTemplateKeys.OrderBy(x => x));
		}

		[Fact]
		public async Task Ensure_TemplateIsCompiled_ForExisting_ProjectItem()
		{
			var project = new EmbeddedRazorProject(typeof(Root).Assembly, "RazorLight.Tests.Assets.Embedded");
			var compiler = TestRazorTemplateCompiler.Create(project: project);

			string templateKey = "Empty.cshtml";
			var result = await compiler.CompileAsync(templateKey);

			Assert.NotNull(result);
			Assert.NotNull(result.TemplateAttribute?.TemplateType);
			Assert.Equal(result.TemplateKey, templateKey);
			Assert.False(result.IsPrecompiled);
		}



		private sealed class TestRazorTemplateCompiler : RazorTemplateCompiler
		{
			public TestRazorTemplateCompiler(
				RazorSourceGenerator sourceGenerator,
				RoslynCompilationService roslynCompilationService,
				RazorLightProject razorLightProject,
				RazorLightOptions razorLightOptions) : base(sourceGenerator, roslynCompilationService, razorLightProject, razorLightOptions)
			{
			}

			public static TestRazorTemplateCompiler Create(RazorLightOptions? options = null, RazorLightProject? project = null)
			{
				var razorOptions = options ?? new RazorLightOptions();
				var metadataManager = new DefaultMetadataReferenceManager();
				var assembly = Assembly.GetCallingAssembly();
				var razorProject = project ?? new EmbeddedRazorProject(assembly);
				var compilerService = new RoslynCompilationService(metadataManager, assembly);
				var generator = new RazorSourceGenerator(Razor6CompilerCompatibility.CreateEngine(), razorProject);

				return new TestRazorTemplateCompiler(generator, compilerService, razorProject, razorOptions);
			}
		}

		private sealed class IndependentlyBlockingProject : RazorLightProject
		{
			public TaskCompletionSource BlockedLookupStarted { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			public TaskCompletionSource ReleaseBlocked { get; } =
				new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			public override async Task<RazorLightProjectItem> GetItemAsync(string templateKey)
			{
				if (templateKey == "blocked")
				{
					BlockedLookupStarted.TrySetResult();
					await ReleaseBlocked.Task;
				}

				return new TextSourceRazorProjectItem(templateKey, templateKey);
			}

			public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey) =>
				Task.FromResult<IEnumerable<RazorLightProjectItem>>(Array.Empty<RazorLightProjectItem>());
		}
	}
}
