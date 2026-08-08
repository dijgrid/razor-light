# Deployment compatibility

RazorLight compiles Razor and C# into a new managed assembly while the application is running.
That capability requires a JIT-enabled .NET runtime, metadata reference files, and dynamic assembly
loading. Deployment modes that remove any of those facilities have a different support boundary.

## Supported matrix

| Deployment mode | Runtime compilation | Required configuration |
| --- | --- | --- |
| Framework-dependent | Supported on Windows and Linux | Set `PreserveCompilationContext` on the entry application. |
| Self-contained | Supported on Windows and Linux | Set `PreserveCompilationContext` and publish for the target runtime identifier. |
| Single-file, self-extracting | Supported on Windows and Linux | Also set `PublishSingleFile` and `IncludeAllContentForSelfExtract`. |
| Single-file, no extraction | Not supported | Bundled assemblies do not expose the files required by the compiler. |
| Trimmed | Runtime compilation is not supported | Keep the RazorLight process untrimmed. |
| Native AOT | Runtime compilation is not supported | Use a JIT-capable process for Razor rendering. |

The repository executes the three supported modes through
`tests/RazorLight.DeploymentProbe` on Windows and Linux. macOS remains covered by the normal build
and unit-test matrix, but is not a claimed deployment-probe platform.

## Application configuration

A framework-dependent or self-contained entry application needs the compilation context:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
</PropertyGroup>
```

For a single-file executable, opt into extraction of the bundled files so RazorLight can read the
dependency context and reference assemblies:

```xml
<PropertyGroup>
  <PreserveCompilationContext>true</PreserveCompilationContext>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>
</PropertyGroup>
```

`IncludeAllContentForSelfExtract` still produces one application bundle, but the .NET host extracts
its contents before startup. This has disk, startup, and temporary-directory implications. A
non-extracting single-file bundle is not compatible with runtime compilation: .NET documents that
`Assembly.Location` is empty for bundled assemblies, and `DependencyContext.Load(Assembly)` cannot
load a bundled assembly's dependency context.

Metadata discovery now ignores dynamic assemblies, empty locations, and missing files. If no usable
references remain, RazorLight reports how to configure `PreserveCompilationContext` or
`AddMetadataReferences` instead of attempting to open an empty path.

## Trimming and Native AOT diagnostics

Runtime-compilation methods carry both `RequiresUnreferencedCode` and `RequiresDynamicCode`:

- a trimmed consumer receives `IL2026` at the RazorLight call site;
- a Native AOT consumer receives `IL3050` at the RazorLight call site;
- an environment without dynamic-code support also receives an actionable
  `PlatformNotSupportedException` before Roslyn emits or loads a template assembly.

These are compatibility diagnostics, not warnings that an application should suppress. The .NET
Native AOT runtime does not support dynamic assembly loading or runtime code generation, both of
which are fundamental to RazorLight's runtime compiler. See Microsoft's
[Native AOT limitations](https://learn.microsoft.com/dotnet/core/deploying/native-aot/),
[trim analysis guidance](https://learn.microsoft.com/dotnet/core/deploying/trimming/trimming-concepts),
and [single-file `Assembly.Location` guidance](https://learn.microsoft.com/dotnet/core/deploying/single-file/warnings/il3000).

The library trim-analysis inventory is intentionally explicit while runtime compilation remains
unsupported:

| Diagnostics | Components | Reason |
| --- | --- | --- |
| `IL2026` | metadata discovery, file-system compiled cache, template compiler and renderer | Loads assemblies and uses the dynamic runtime binder. |
| `IL2055`, `IL2060`, `IL2067`, `IL2070`, `IL2072`, `IL2075` | property activation, dependency injection, compiled-template factories, debug-symbol detection | Template and model types are discovered at runtime and cannot carry static linker guarantees. |

CI reruns the analysis and fails if a new diagnostic category appears. The public runtime-compilation
annotations move the actionable warning to the consumer call site without globally suppressing the
internal evidence.

## Precompiled alternatives

`RazorLightEngineBuilder.CreatePrecompiled` is the static runtime entry point for build-produced page
factories. It does not construct or fall back to the Razor/Roslyn compilation graph. A trimmed,
self-contained, single-file deployment probe verifies this path on the supported Windows and Linux
matrix; see [precompiled-only execution](precompiled-only.md) for the workflow and limitations.

Native AOT is not claimed. Dynamic loading of separately deployed template assemblies and other
generated-page runtime behavior still require an explicit executable AOT proof.
