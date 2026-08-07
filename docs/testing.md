# Testing and quality gates

The repository supports .NET 10 and runs the complete test suite on Windows, Linux, and macOS.
The CI workflow restores once, performs a Release build, runs both test projects, validates the
NuGet library and tool packages, verifies deterministic DLL/PDB output and Source Link, and uploads
Cobertura coverage reports plus reviewable release-candidate package artifacts.

Run the same checks locally from the repository root:

```powershell
dotnet restore RazorLight.sln
dotnet build RazorLight.sln --configuration Release --no-restore
dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --configuration Release --no-build
dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-build
dotnet tool restore
pwsh ./scripts/Test-DeterministicBuild.ps1
dotnet pack src/RazorLight/RazorLight.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/RazorLight.Precompile/RazorLight.Precompile.csproj --configuration Release --no-build --output artifacts/packages
pwsh ./scripts/Validate-Packages.ps1 -PackageDirectory artifacts/packages -Version 3.0.0
```

## Initial coverage baseline

Coverage is collected with `coverlet.collector` in portable Cobertura XML. The local Windows
baseline recorded on 2026-08-06 is:

| Suite | Tests | Line coverage | Branch coverage |
| --- | ---: | ---: | ---: |
| RazorLight | 183 | 64.37% | 51.19% |
| RazorLight.Precompile | 118 | 46.32% | 32.17% |

This is an observation baseline, not a minimum threshold. It makes regressions visible without
blocking maintenance work until representative cross-platform history exists. A later task can
introduce an enforced threshold based on that history.

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
