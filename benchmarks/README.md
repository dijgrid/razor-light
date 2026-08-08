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
