using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace RazorLight.Benchmarks;

/// <summary>
/// Runs deliberately one-shot scaling evaluations that are too large for a normal microbenchmark.
/// Invoke each scenario in a fresh process so working-set and allocation figures are comparable.
/// </summary>
internal static class ScalingEvaluation
{
	private const int TemplateCount = 1_000;

	public static async Task RunAsync(string[] scenarios)
	{
		if (scenarios.Length != 1)
		{
			throw new ArgumentException("Specify exactly one scenario: large-5, large-10, large-15, or templates-1000.");
		}

		object result = scenarios[0] switch
		{
			"large-5" => await MeasureLargeTemplateAsync(5),
			"large-10" => await MeasureLargeTemplateAsync(10),
			"large-15" => await MeasureLargeTemplateAsync(15),
			"templates-1000" => await MeasureTemplateCardinalityAsync(),
			_ => throw new ArgumentException($"Unknown scaling scenario '{scenarios[0]}'."),
		};

		Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
	}

	private static async Task<object> MeasureLargeTemplateAsync(int sizeMiB)
	{
		CollectGarbage();
		using Process process = Process.GetCurrentProcess();
		long workingSetBefore = process.WorkingSet64;
		int literalCharacters = checked(sizeMiB * 1024 * 1024);
		string source = new string('x', literalCharacters) + "\n@Model";
		await using IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();

		Measurement cold = await MeasureAsync(() =>
			engine.CompileRenderStringAsync($"large-{sizeMiB}", source, "END"));
		int expectedOutputLength = literalCharacters + 1 + 3;
		if (cold.OutputLength != expectedOutputLength)
		{
			throw new InvalidOperationException(
				$"The large template output length was {cold.OutputLength}, expected {expectedOutputLength}.");
		}

		Measurement cached = await MeasureAsync(() =>
			engine.CompileRenderAsync($"large-{sizeMiB}", "END"));
		process.Refresh();

		return new
		{
			Scenario = $"large-{sizeMiB}",
			TemplateSizeMiB = sizeMiB,
			SourceCharacters = source.Length,
			ColdCompileAndRender = cold,
			CachedRender = cached,
			WorkingSetBeforeMiB = ToMiB(workingSetBefore),
			WorkingSetAfterMiB = ToMiB(process.WorkingSet64),
			PeakWorkingSetMiB = ToNullableMiB(process.PeakWorkingSet64),
			Environment = GetEnvironment(),
		};
	}

	private static async Task<object> MeasureTemplateCardinalityAsync()
	{
		CollectGarbage();
		using Process process = Process.GetCurrentProcess();
		long workingSetBefore = process.WorkingSet64;
		await using IRazorLightEngine engine = BenchmarkEnvironment.CreateStringEngine();
		var outputs = new List<string>(TemplateCount);

		Measurement cold = await MeasureAsync(async () =>
		{
			for (int index = 0; index < TemplateCount; index++)
			{
				outputs.Add(await engine.CompileRenderStringAsync(
					$"template-{index.ToString(CultureInfo.InvariantCulture)}",
					$"Template {index.ToString(CultureInfo.InvariantCulture)}: @Model",
					"value"));
			}
			return string.Join('|', outputs);
		});
		if (outputs.Count != TemplateCount || outputs[999] != "Template 999: value")
		{
			throw new InvalidOperationException("The 1,000-template evaluation did not preserve output identity.");
		}

		outputs.Clear();
		Measurement cached = await MeasureAsync(async () =>
		{
			for (int index = 0; index < TemplateCount; index++)
			{
				outputs.Add(await engine.CompileRenderAsync(
					$"template-{index.ToString(CultureInfo.InvariantCulture)}",
					"cached"));
			}
			return string.Join('|', outputs);
		});
		process.Refresh();

		return new
		{
			Scenario = "templates-1000",
			TemplateCount,
			ColdCompileAndRender = cold,
			CachedRender = cached,
			WorkingSetBeforeMiB = ToMiB(workingSetBefore),
			WorkingSetAfterMiB = ToMiB(process.WorkingSet64),
			PeakWorkingSetMiB = ToNullableMiB(process.PeakWorkingSet64),
			Environment = GetEnvironment(),
		};
	}

	private static async Task<Measurement> MeasureAsync(Func<Task<string>> operation)
	{
		long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
		var stopwatch = Stopwatch.StartNew();
		string output = await operation();
		stopwatch.Stop();
		long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
		return new Measurement(
			Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3),
			Math.Round(ToMiB(allocated), 3),
			output.Length);
	}

	private static void CollectGarbage()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private static double ToMiB(long bytes) => Math.Round(bytes / 1024d / 1024d, 3);
	private static double? ToNullableMiB(long bytes) => bytes > 0 ? ToMiB(bytes) : null;

	private static object GetEnvironment() => new
	{
		Runtime = RuntimeInformation.FrameworkDescription,
		OperatingSystem = RuntimeInformation.OSDescription,
		Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
		LogicalProcessors = Environment.ProcessorCount,
		ServerGarbageCollection = GCSettings.IsServerGC,
	};

	internal sealed record Measurement(double ElapsedMilliseconds, double ManagedAllocatedMiB, int OutputLength);
}
