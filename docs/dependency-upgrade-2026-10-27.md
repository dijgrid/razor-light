# Stable dependency upgrade review — 2026-10-27

Local validation on Almere (macOS arm64), using SDK 10.0.400 selected by `global.json`,
.NET runtime 10.0.11. Base: remote `master` at `3c38597ebca7f7607bd0b4b978bcfbdcbbe66f63`
(merged PR #47). Branch: `codex/package-upgrades-20261027`.

All 30 central NuGet package IDs and the one repository CLI were checked against NuGet's
[v3 flat-container registry](https://api.nuget.org/v3/index.json), excluding prereleases.
18 central versions were upgraded. No runtime source, public API, target framework, SDK pin,
release version, credential, or security configuration changes were required.

## Version changes

| Package | Before | After |
| --- | --- | --- |
| coverlet.collector | 10.0.1 | 10.1.0 |
| Microsoft.CodeAnalysis.Common | 5.6.0 | 5.9.0 |
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 |
| Microsoft.EntityFrameworkCore | 10.0.10 | 10.0.14 |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.DependencyModel | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.Caching.Memory | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.FileProviders.Physical | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.Hosting | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.Options | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.Primitives | 10.0.10 | 10.0.14 |
| Microsoft.Extensions.FileSystemGlobbing | 10.0.10 | 10.0.14 |
| Microsoft.NET.Test.Sdk | 18.8.1 | 18.11.0 |
| Microsoft.SourceLink.GitHub | 10.0.402 | 10.0.403 |
| Moq | 4.20.72 | 4.21.0 |
| xunit.runner.visualstudio | 3.1.5 | 4.0.1 |
| Xunit.SkippableFact | 1.5.61 | 1.5.85 |

## Already at the latest stable version for the existing package ID

| Package/tool | Version |
| --- | --- |
| BenchmarkDotNet | 0.15.8 |
| Microsoft.AspNetCore.Mvc.Razor.Extensions | 6.0.36 |
| Microsoft.Azure.Functions.Worker | 2.52.0 |
| Microsoft.Azure.Functions.Worker.Extensions.Http | 3.3.0 |
| Microsoft.Azure.Functions.Worker.Sdk | 2.1.0 |
| Microsoft.CodeAnalysis.PublicApiAnalyzers | 5.6.0 |
| Microsoft.CodeAnalysis.Razor | 6.0.36 |
| Mono.Cecil | 0.11.6 |
| Newtonsoft.Json | 13.0.4 |
| Verify.Xunit | 31.12.5 |
| xunit | 2.9.3 |
| ZstdNet | 1.5.7 |
| sourcelink | 3.1.1 |

## Inventory and compatibility decisions

The 13 tracked projects are:

- Libraries/tool: `src/RazorLight`, `src/RazorLight.Html`, `src/RazorLight.Precompile`.
- Test suites: `tests/RazorLight.Tests`, `tests/RazorLight.Precompile.Tests`.
- Probes: `tests/RazorLight.DeploymentProbe`, `tests/RazorLight.WorkerProbe`,
  `tests/RazorLight.DesktopProbe`, `tests/RazorLight.PrecompiledProbe`.
- Samples: `samples/RazorLight.Samples/Samples.EntityFrameworkProject.csproj` and
  `samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj`.
- `benchmarks/RazorLight.Benchmarks` and the tracked, otherwise ignored `sandbox/RazorLight.Sandbox`.

The main solution contains 11 projects. The Azure Functions sample and DeploymentProbe are
validated separately. The Functions SDK also generates a `WorkerExtensions` net8.0 project;
its dependencies were audited using the same disabled Directory.* imports as the SDK.
No tracked lock files or package-version overrides were found. Central versions come from
`Directory.Packages.props`; `.config/dotnet-tools.json` contains only `sourcelink` 3.1.1.
The repository has root `AGENTS.md`, and no applicable `.agents/skills` directory was present.

- **xUnit runner 4.0.1:** accepted the major update. Its official
  [package documentation](https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.1)
  supports xUnit 1.9.2 and later; all existing xUnit 2 tests and coverage collection passed.
- **Roslyn 5.9.0:** updated Common and CSharp together. Rendering, compilation diagnostics,
  snapshots, precompilation, public API checks, package compatibility and consumer tests passed.
  PublicApiAnalyzers stays at its independently versioned latest stable 5.6.0.
- **xUnit 2 / Verify.Xunit:** the latest versions of these IDs are marked legacy by NuGet.
  Their successors are `xunit.v3` and `Verify.XunitV3`, not in-place version bumps. The
  [xUnit migration guide](https://xunit.net/docs/getting-started/v3/migration) requires executable
  test projects and describes lifecycle/discovery changes. Both suites, snapshot integration,
  the xUnit 2-specific SkippableFact dependency, and coverage need coordinated migration and
  validation. No claim is made that migration is impossible; it is deferred as TASK-044 rather
  than silently expanding this compatible package refresh.
- **Razor 6.0.36 packages:** these remain the newest stable packages under their existing IDs.
  A newer SDK compiler integration would be a separate architecture/API migration, not a
  package-version update. The current rendering and compilation checks pass.
- **Source Link CLI:** latest stable 3.1.1 has no newer same-ID release to apply; its bundled
  dependencies cannot be changed by updating central versions. Replacement is TASK-045.

## Validation results

| Check | Result |
| --- | --- |
| Solution restore, all dependency severities | Passed |
| Release solution build, warnings as errors | Passed, zero warnings/errors; includes desktop cross-build, benchmarks and sandbox |
| Runtime tests with coverage | 332 passed, none skipped; 74.29% lines / 60.09% branches (floors 73% / 59%) |
| Precompile tests with coverage | 78 passed, none skipped; 80.92% lines / 72.54% branches (floors 80% / 70%) |
| Whitespace and IDE0005/IDE0065 formatting verification | Passed |
| Azure Functions sample Release build, warnings as errors | Passed |
| Entity Framework sample execution | Passed |
| DeploymentProbe Release build and execution | Passed |
| Worker and precompiled-only probe execution | Passed |
| Deterministic DLL/PDB build script | Passed for all six outputs |
| Three package and three symbol-package builds | Passed, including configured API/package compatibility validation |
| Package layout, nuspec, symbols and Source Link metadata script | Passed |
| Clean consumer render and installed CLI smoke tests | Passed |
| Source Link document resolution for all three PDBs | Passed |
| Trim-warning inventory and required trimming/AOT diagnostics | Passed |
| Outdated direct dependencies, solution and Functions sample | No newer stable versions for existing IDs |
| Direct/transitive vulnerability audit of all 13 projects and generated WorkerExtensions | No advisories reported; no feed errors |

The precompile total differs from the historical Windows total of 134 because two path-separator
theories generate 30 rows each on Windows and two each on Unix: 56 fewer rows on macOS.
No tests were removed or skipped.

The initial sandboxed build stalled and was cancelled; the build with normal local process access
passed. The initial direct restore of generated WorkerExtensions omitted the SDK's import flags
and failed with NU1008; rerunning with those flags passed. The installed CLI initially selected an
old `/usr/local/share/dotnet` runtime; setting `DOTNET_ROOT` and `DOTNET_ROOT_ARM64` to the installed
Homebrew .NET 10 path for that process made the smoke test pass. No persistent environment change
was made. A separate local-feed-only tool install also passed, and cached consumer library package
hashes matched the exact locally packed artifacts.

**Not run:** Windows/Linux execution and deployment matrices, Windows Forms execution, the Azure
Functions host, and a performance benchmark run. `Test-DeploymentModes.ps1` explicitly supports
Windows/Linux only; macOS ran the direct probes above. At the time of this local validation, CI for the new dependency state had not
run; consult the draft PR checks for subsequent CI results. A passing CI result for base PR #47 does not validate these
upgrades. Packages remain local validation artifacts at unchanged version 3.0.1, not release output.

## Full audit exception: repository validation tool

The solution/package audits do not inspect a dotnet tool's bundled runtime dependencies.
The `sourcelink` 3.1.1 dependency manifest (66 entries) was separately compared with the official
NuGet vulnerability feed using the SDK's `NuGet.Versioning.VersionRange` matching implementation.
It contains two high-severity matches:

| Dependency recorded by tool | Advisory | Scope |
| --- | --- | --- |
| Newtonsoft.Json 10.0.3 | [GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr) | Tool actually bundles this DLL; denial-of-service advisory, fixed in 13.0.1 |
| System.Text.RegularExpressions 4.3.0 | [GHSA-cmhx-cq75-c4mj](https://github.com/advisories/GHSA-cmhx-cq75-c4mj) | Dependency-manifest match; no regex DLL is bundled, so actual execution uses the selected shared framework; runtime exploitability was not established |

These are in the validation CLI, not the updated RazorLight consumer dependency graph. The full
repository audit therefore cannot be described as entirely clean. No advisory was suppressed.
TASK-045 records replacement work that must retain PDB metadata and source-resolution validation.

## Existing dependency PR overlap (read-only review)

- #50: six Microsoft.Extensions packages.
- #48: Moq; #46: Microsoft.NET.Test.Sdk; #45: xunit.runner.visualstudio.
- #40: coverlet.collector; #33: Xunit.SkippableFact.
- #31: Common and CSharp Roslyn group; #32: Common alone (duplicates part of #31).
- #41 / #42: upload/download-artifact GitHub Actions updates remain separate existing work;
  workflow action pins were not changed by this NuGet run.

These existing PRs were not changed, commented on, or merged. The consolidated upgrade overlaps
them and should be reconciled with them during review. Package publication and deployment are
outside this dependency refresh.
