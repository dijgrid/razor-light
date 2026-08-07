# Dependency and restore policy

Package versions are managed in the repository-root `Directory.Packages.props`. Project files state
which packages they use; version-family decisions are reviewed once in the central file. Dependabot
scans the repository root each Monday at 06:00 America/Denver and updates the central declarations,
not per-project versions.

`NuGet.config` clears machine-wide sources and permits only the HTTPS NuGet.org v3 feed. This keeps
developer and CI restores reproducible and prevents an unrelated private or offline source from
silently satisfying a package ID.

## Version policy

- .NET runtime and `Microsoft.Extensions` packages follow the .NET 10 servicing line.
- Roslyn compiler packages use the current 5.6 line.
- The Razor compiler compatibility packages use `6.0.36`, the final published versions of those
  package IDs. They have no reported NuGet advisories and run with the .NET 10 framework and current
  Roslyn packages, but they are a known compatibility layer rather than a current Razor product
  surface. Replacing their internal APIs requires a separately baselined compiler integration.
- Test and build tools use current stable releases compatible with .NET 10.
- The precompile tool no longer depends on the abandoned `ManyConsole` and `Glob` packages. Its
  command parser is local and its glob behavior uses `Microsoft.Extensions.FileSystemGlobbing`.

## Dependabot update policy

Only packages that share a release train or compatibility boundary are grouped:

| Group | Packages | Rationale |
| --- | --- | --- |
| `razor-compiler` | ASP.NET Core Razor extensions and CodeAnalysis Razor | Both packages are the retained Razor 6 compiler compatibility layer. |
| `roslyn` | CodeAnalysis Common and CSharp | Roslyn compiler assemblies must remain on the same version. |
| `entity-framework` | Entity Framework Core packages | The maintained EF sample uses one EF Core release train. |
| `microsoft-extensions` | Microsoft.Extensions packages | Runtime extensions follow the .NET 10 servicing line. |
| `azure-functions` | Azure Functions Worker packages | The Functions sample's worker, SDK, and HTTP extension are validated together. |
| `xunit` | xUnit core and Visual Studio runner | The runner is validated with the test framework it discovers. |
| `nunit` | NUnit and NUnit adapter | The precompile tests require a compatible framework and adapter pair. |

Unmatched packages and GitHub Actions update independently. There are no permanent ignore rules.
Every Dependabot pull request runs the same unfiltered `pull_request` CI matrix as a maintainer pull
request, including builds, both test suites, package validation, samples, and README generation.
Dependency-manifest changes also run the dedicated audit workflow.

## Audit

Repository-wide NuGet audit settings explicitly inspect direct and transitive dependencies and
report moderate, high, and critical advisories. The dedicated `Dependency Audit` workflow runs after
dependency changes and every Tuesday at 13:30 UTC. In that workflow, advisories at the configured
threshold and failures to obtain vulnerability data are errors.

Run the same audit after changing package versions:

```powershell
dotnet restore RazorLight.sln --force-evaluate -p:AuditPipeline=true
dotnet restore samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj --force-evaluate -p:AuditPipeline=true
dotnet package list --project RazorLight.sln --vulnerable --include-transitive --no-restore
dotnet package list --project samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj --vulnerable --include-transitive --no-restore
dotnet package list --project RazorLight.sln --outdated --no-restore
```

The TASK-004 audit completed with only `https://api.nuget.org/v3/index.json` as a source, no known
vulnerable packages, and no outdated direct packages. In particular, the inherited
`Microsoft.Extensions.Caching.Memory` advisory and old runtime-package advisories are absent.

## Alerts and suppressions

Treat a Dependabot alert or audit failure as maintenance work: identify the direct dependency with
`dotnet nuget why`, update the owning package family, and run the complete CI baseline. Security
updates may be merged ahead of the weekly version-update cadence after review.

Suppressing an advisory is a last resort. A suppression requires a PlanFS decision recording the
advisory URL, affected dependency path, applicability analysis, compensating controls, owner, and an
explicit expiry or review date. Use an exact `NuGetAuditSuppress` entry for the advisory; do not use
broad `NoWarn` entries. Dependabot `ignore` entries require the same record and must identify the
specific dependency and time-bounded version range. Remove the suppression when the decision
expires or a fixed compatible version becomes available.
