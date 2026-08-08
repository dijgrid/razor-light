using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace RazorLight.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class CompilationBenchmarks
{
	private static readonly string LargeTemplate = string.Concat(Enumerable.Repeat("0123456789", 12_800)) + " @Model";
	private int key;

	[Benchmark(Baseline = true)]
	public async Task<string> ColdStringCompile()
	{
		IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();
		try { return await engine.CompileRenderStringAsync("cold-" + Interlocked.Increment(ref key), "Hello @Model", "world"); }
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}

	[Benchmark]
	public async Task<string> ColdFileCompile()
	{
		IRazorLightEngine engine = BenchmarkEnvironment.CreateFileEngine();
		try { return await engine.CompileRenderAsync("Simple.cshtml", "world"); }
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}

	[Benchmark]
	public async Task<string> ColdEmbeddedCompile()
	{
		IRazorLightEngine engine = new RazorLightEngineBuilder()
			.UseEmbeddedResourcesProject(typeof(Program))
			.UseMemoryCachingProvider()
			.Build();
		try { return await engine.CompileRenderAsync("Templates.Embedded.cshtml", "world"); }
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}

	[Benchmark]
	public async Task<string> LargeStringCompile()
	{
		IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();
		try { return await engine.CompileRenderStringAsync("large-" + Interlocked.Increment(ref key), LargeTemplate, "world"); }
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}

	[Benchmark]
	public async Task<string[]> SameKeyColdConcurrency()
	{
		IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();
		string templateKey = "same-" + Interlocked.Increment(ref key);
		try
		{
			return await Task.WhenAll(Enumerable.Range(0, 8)
				.Select(_ => engine.CompileRenderStringAsync(templateKey, "Hello @Model", "world")));
		}
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}

	[Benchmark]
	public async Task<string[]> UnrelatedKeyColdConcurrency()
	{
		IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();
		int run = Interlocked.Increment(ref key);
		try
		{
			return await Task.WhenAll(Enumerable.Range(0, 8)
				.Select(index => engine.CompileRenderStringAsync($"unrelated-{run}-{index}", "Hello @Model", "world")));
		}
		finally { BenchmarkEnvironment.DisposeEngine(engine); }
	}
}
