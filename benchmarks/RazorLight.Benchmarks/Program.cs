using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace RazorLight.Benchmarks;

public static class Program
{
	public static async Task Main(string[] args)
	{
		if (args.FirstOrDefault() == "--scaling")
		{
			await ScalingEvaluation.RunAsync(args.Skip(1).ToArray());
			return;
		}

		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
