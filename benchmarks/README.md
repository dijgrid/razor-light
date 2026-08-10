# RazorLight performance benchmarks

This BenchmarkDotNet suite is the versioned performance fixture for the .NET 10 release line. Its
templates, concurrency widths, large-source size, ShortRun job, and allocation diagnoser live in the
repository so results can be reproduced and compared with the full environment metadata emitted by
BenchmarkDotNet.

The scenarios correspond to the areas changed in TASK-034 through TASK-037: per-key compilation
coordination, reusable-page lifecycle, deterministic disk hashing, cached dependency injection, and
engine construction. The initial baseline includes a historical comparison against the commit before
those changes, built and measured with this same harness. Future optimization claims must likewise
include results from the commit before and after the change, using the same machine, power plan,
filter, and BenchmarkDotNet configuration.

Run all scenarios:

```powershell
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj `
  --configuration Release -- --exporters json markdown
```

Do not turn a single noisy ShortRun measurement into a pass/fail budget. Preserve raw artifacts,
repeat suspicious results, compare managed allocations as well as elapsed time, and introduce a CI
budget only after stable runner history shows a defensible threshold.

## Large-input scaling evaluation

The one-shot scaling harness complements the microbenchmarks with workloads that are intentionally
too large for repeated BenchmarkDotNet iterations. Run each scenario in a fresh process so working
set and allocation totals are not inherited from an earlier dynamic compilation:

```shell
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj \
  --configuration Release -- --scaling large-5
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj \
  --configuration Release -- --scaling large-10
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj \
  --configuration Release -- --scaling large-15
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj \
  --configuration Release -- --scaling templates-1000
```

Each command verifies output identity and emits JSON containing cold compile/render and cached
render time, total managed bytes allocated during each phase, output size, process working set, and
runtime environment. Managed allocation is cumulative activity rather than retained memory.
Working set includes the runtime and compiler and may not fall after disposal because runtime-loaded
assemblies live for the process lifetime. `PeakWorkingSetMiB` is `null` where the operating system
does not expose that process counter.

See [the stable 3.0 scaling evaluation](scaling-2026-08-10.md) for the release-gate measurements and
their operational interpretation.
