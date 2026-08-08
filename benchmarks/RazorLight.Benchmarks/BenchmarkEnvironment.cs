using System;
using System.IO;
using RazorLight.Caching;

namespace RazorLight.Benchmarks;

internal static class BenchmarkEnvironment
{
	public static string TemplateRoot => Path.Combine(AppContext.BaseDirectory, "Templates");

	public static IRazorLightEngine CreateFileEngine(ICachingProvider? cache = null)
	{
		var builder = new RazorLightEngineBuilder()
			.UseFileSystemProject(TemplateRoot)
			.SetOperatingAssembly(typeof(BenchmarkEnvironment).Assembly);
		return cache == null
			? builder.UseMemoryCachingProvider().Build()
			: builder.UseCachingProvider(cache).Build();
	}

	public static IRazorLightEngine CreateStringEngine() =>
		new RazorLightEngineBuilder()
			.UseNoProject()
			.UseMemoryCachingProvider()
			.SetOperatingAssembly(typeof(BenchmarkEnvironment).Assembly)
			.Build();

	public static void DisposeEngine(IRazorLightEngine engine) => (engine as IDisposable)?.Dispose();

	public static void DisposeObject(object value) => (value as IDisposable)?.Dispose();
}

public sealed class PrefixService
{
	public string Value => "service:";
}
