# Testing and quality gates

The repository supports .NET 10 and runs the complete test suite on Windows, Linux, and macOS.
The CI workflow restores once, performs a Release build, runs both test projects, validates the
NuGet library and tool packages, verifies deterministic DLL/PDB output and Source Link, and uploads
Cobertura coverage reports plus reviewable release-candidate package artifacts.

Run the same checks locally from the repository root:

```powershell
dotnet restore RazorLight.sln
dotnet build RazorLight.sln --configuration Release --no-restore
pwsh ./scripts/Test-Coverage.ps1 -NoBuild
dotnet tool restore
pwsh ./scripts/Test-DeterministicBuild.ps1
dotnet pack src/RazorLight/RazorLight.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/RazorLight.Html/RazorLight.Html.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/RazorLight.Precompile/RazorLight.Precompile.csproj --configuration Release --no-build --output artifacts/packages
pwsh ./scripts/Validate-Packages.ps1 -PackageDirectory artifacts/packages -Version 3.0.0
```

## Coverage ratchet

`scripts/Test-Coverage.ps1` runs both xUnit suites, reads their portable Cobertura reports, prints one
consistent summary, and enforces the versioned floors in `eng/coverage-baseline.json`. CI runs this
on Windows, Linux, and macOS. Raise a floor when representative cross-platform results support it;
never lower one merely to make a change pass.

The accepted 2026-08-08 floors and current Windows observations are:

| Component | Line floor | Branch floor | Observed line | Observed branch |
| --- | ---: | ---: | ---: | ---: |
| RazorLight | 73.0% | 59.0% | 73.96% | 60.00% |
| RazorLight.Precompile | 80.0% | 70.0% | 80.92% | 71.15% |

The floors apply to the named production assembly in each report, rather than averaging dependencies
or helper libraries into a misleading repository-wide percentage.

## Performance benchmarks

The BenchmarkDotNet project under `benchmarks/RazorLight.Benchmarks` measures cold string, file, and
embedded compilation; large sources; same-key and unrelated-key concurrency; cached rendering with
and without dependency injection; layout/include-heavy rendering; deterministic disk-cache loads;
and repeated engine construction/disposal. Every benchmark includes managed allocation measurements,
and BenchmarkDotNet records the OS, SDK, runtime, JIT, GC, and processor with each result.

Run the short, reproducible suite on an otherwise idle machine:

```powershell
dotnet run --project benchmarks/RazorLight.Benchmarks/RazorLight.Benchmarks.csproj `
  --configuration Release -- --exporters json markdown
```

Use `--filter "*CachedRender*"` for a focused run. The manual GitHub workflow retains the same JSON
and Markdown artifacts. The initial measurements are an observation baseline: no timing budget is
enforced until multiple runs on stable hardware establish normal variance. Treat a change as a
candidate regression only when repeated measurements exceed both normal noise and a material effect
size; allocation regressions are generally less noisy and should be investigated immediately.

## Reliability inventory

- There are no skipped or platform-filtered tests.
- The renderer assertions normalize CR and LF independently so they validate output rather than an
  operating system's newline convention.
- Culture-sensitive precompile fixtures set explicit `en-US` date and time patterns so ICU and NLS
  implementations produce the same expected output.
- The test tree contains no `Thread.Sleep` or `Task.Delay` timing dependencies.
- `PrecompileTests` records elapsed time for diagnostic output only; it does not assert a performance
  threshold.
- The race-condition fixture remains concurrent internally, but runs in a non-parallel xUnit
  collection so unrelated tests cannot concurrently mutate the shared Razor compilation engine.
