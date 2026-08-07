# Dependency and restore policy

Package versions are managed in the repository-root `Directory.Packages.props`. Project files state
which packages they use; version-family decisions are reviewed once in the central file. Closely
coupled Microsoft runtime, Razor, Roslyn, Entity Framework, test, and Azure Functions packages are
grouped by Dependabot so servicing updates arrive as coherent changes.

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

## Audit

Run both direct and transitive audits after changing package versions:

```powershell
dotnet restore RazorLight.sln --force
dotnet list RazorLight.sln package --vulnerable --include-transitive
dotnet list RazorLight.sln package --outdated
dotnet list samples/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample/FunctionApp-WebMvc-Sample.csproj package --vulnerable --include-transitive
```

The TASK-004 audit completed with only `https://api.nuget.org/v3/index.json` as a source, no known
vulnerable packages, and no outdated direct packages. In particular, the inherited
`Microsoft.Extensions.Caching.Memory` advisory and old runtime-package advisories are absent.
