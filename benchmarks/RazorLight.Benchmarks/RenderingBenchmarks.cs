using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using RazorLight.Caching;
using RazorLight.Extensions;

namespace RazorLight.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class RenderingBenchmarks
{
	private IRazorLightEngine engine = null!;
	private IRazorLightEngine injectedEngine = null!;
	private ServiceProvider services = null!;

	[GlobalSetup]
	public async Task Setup()
	{
		engine = BenchmarkEnvironment.CreateFileEngine();
		await engine.CompileRenderAsync("Simple.cshtml", "warmup");
		await engine.CompileRenderAsync("Composition.cshtml", "warmup");
		string cacheRoot = Path.Combine(AppContext.BaseDirectory, "disk-cache");
		var diskCache = new FileSystemCachingProvider(
			BenchmarkEnvironment.TemplateRoot,
			cacheRoot,
			FileHashCachingStrategy.Instance);
		IRazorLightEngine diskEngine = BenchmarkEnvironment.CreateFileEngine(diskCache);
		try
		{
			await diskEngine.CompileRenderAsync("Simple.cshtml", "warmup");
		}
		finally
		{
			BenchmarkEnvironment.DisposeEngine(diskEngine);
			BenchmarkEnvironment.DisposeObject(diskCache);
		}

		var collection = new ServiceCollection();
		collection.AddSingleton<PrefixService>();
		collection.AddRazorLight(BenchmarkEnvironment.CreateStringEngine);
		services = collection.BuildServiceProvider();
		injectedEngine = services.GetRequiredService<IRazorLightEngine>();
		await injectedEngine.CompileRenderStringAsync(
			"injected",
			"@inject RazorLight.Benchmarks.PrefixService Prefix\n@Prefix.Value@Model",
			"warmup");
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		services.Dispose();
		BenchmarkEnvironment.DisposeEngine(engine);
	}

	[Benchmark(Baseline = true)]
	public Task<string> CachedRender() => engine.CompileRenderAsync("Simple.cshtml", "world");

	[Benchmark]
	public Task<string> CachedRenderWithDependencyInjection() =>
		injectedEngine.CompileRenderAsync("injected", "world");

	[Benchmark]
	public Task<string> LayoutAndIncludes() => engine.CompileRenderAsync("Composition.cshtml", "world");

	[Benchmark]
	public void EngineConstructionAndDisposal()
	{
		IRazorLightEngine temporary = BenchmarkEnvironment.CreateStringEngine();
		BenchmarkEnvironment.DisposeEngine(temporary);
	}

	[Benchmark]
	public async Task<string> DeterministicDiskCacheLoad()
	{
		string cacheRoot = Path.Combine(AppContext.BaseDirectory, "disk-cache");
		var cache = new FileSystemCachingProvider(
			BenchmarkEnvironment.TemplateRoot,
			cacheRoot,
			FileHashCachingStrategy.Instance);
		IRazorLightEngine diskEngine = BenchmarkEnvironment.CreateFileEngine(cache);
		try { return await diskEngine.CompileRenderAsync("Simple.cshtml", "world"); }
		finally
		{
			BenchmarkEnvironment.DisposeEngine(diskEngine);
			BenchmarkEnvironment.DisposeObject(cache);
		}
	}
}
