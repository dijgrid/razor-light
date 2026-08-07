# Razor compiler integration decision

This technical spike records the compiler boundary selected for the .NET 10 release line. The
decision was reviewed on 2026-08-07 and must be revisited when the target framework changes or
Microsoft publishes a supported runtime Razor compiler API.

## Requirements and baseline

RazorLight compiles strings, database records, files, embedded resources, and custom project items
after the consuming application has started. A replacement must therefore offer a supported
in-process API for turning arbitrary Razor input into C#; build-time compilation alone cannot
preserve that contract.

The inherited runtime graph contains `Microsoft.AspNetCore.Mvc.Razor.Extensions` 6.0.36 and
`Microsoft.CodeAnalysis.Razor` 6.0.36. NuGet identifies both as MIT-licensed packages and shows
6.0.36 as the highest published 6.x version of each package:

- [`Microsoft.AspNetCore.Mvc.Razor.Extensions`](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Razor.Extensions/)
- [`Microsoft.CodeAnalysis.Razor`](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Razor/)

Before changing the boundary, `CurrentCompilerCompatibilityTest` captures stable generated-code
fragments and the `RZ1013` diagnostic for a malformed `@model` directive. The broader regression
baseline covers public API, model/import behavior, layouts, includes, rendering, and compilation
errors. Baselines intentionally avoid a byte-for-byte generated file because generated paths,
checksums, and formatting are compiler implementation details.

## Options considered

| Option | Packaging and support | Risk | Decision |
| --- | --- | --- | --- |
| .NET 10 Razor SDK | The supported SDK compiles Razor at build and publish time. Microsoft documents that Razor language versions are coupled to their runtime. | It cannot compile a newly supplied string or database template in-process after deployment. Calling the SDK would also require the SDK, temporary projects, and process execution in production. | Use later for precompilation work; it cannot replace runtime compilation. |
| ASP.NET Core runtime-compilation package 10.x | A current package exists, but Microsoft marks its APIs obsolete in .NET 10 and recommends build-time compilation. Its NuGet graph still depends on the Razor 6 extension and CodeAnalysis package IDs. | It is MVC-specific, disables Hot Reload, does not remove the inherited compiler layer, and is explicitly not a production recommendation. | Rejected. |
| Load compiler assemblies from the installed SDK | The .NET 10 SDK contains current Razor source-generator and tooling assemblies, but not a supported runtime library contract. | SDK layout and internal APIs can change during servicing; framework-dependent apps need not deploy an SDK. Loading by path would make deployments machine-dependent. | Rejected. |
| Vendor or republish current Razor compiler source | .NET source and library packages are MIT licensed, so licensing permits a maintained fork with attribution and notices. | Razor is tightly coupled to its runtime and relies heavily on internal APIs. Vendoring transfers API, security-servicing, packaging, and generated-code compatibility ownership to this project. | Deferred unless a separately scoped maintenance commitment is approved. |
| Retain the Razor 6 packages behind a compatibility adapter | Preserves the existing in-process contract and public types while current Roslyn compiles generated code. | The Razor parser is not the .NET 10 Razor product and may reject future Razor grammar. It needs explicit tests and cannot be mistaken for a supported current compiler API. | Selected as the bounded interim architecture. |

Supporting sources:

- The [ASP.NET Core Razor SDK documentation](https://learn.microsoft.com/en-us/aspnet/core/razor-pages/sdk?view=aspnetcore-10.0)
  describes build/publish compilation and the Razor language/runtime coupling.
- The [.NET 10 runtime-compilation breaking change](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/razor-runtime-compilation-obsolete?view=aspnetcore-10.0)
  says runtime compilation is obsolete and does not receive new features.
- The [10.0.10 runtime-compilation package graph](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation/10.0.10)
  still lists the Razor 6 package families as dependencies.
- The [.NET licensing summary](https://github.com/dotnet/core/blob/main/license-information.md)
  records MIT licensing for .NET source and library packages.

## Selected boundary and language policy

`Razor6CompilerCompatibility` is the only default-engine construction boundary. It registers
RazorLight's directives, classifier and instrumentation passes, verifies that Razor compiler major
version 6 was loaded, and returns the configured engine. Consumers may still supply a `RazorEngine`
through the inherited public API; those Razor types cannot be removed before a separately reviewed
major API migration.

Razor parsing remains on the compatibility packages, but generated C# is parsed and compiled by the
maintained Roslyn package line. `RoslynCompilationService` always selects `CSharp14`, the language
version supported by .NET 10, instead of inheriting a possibly older or invalid value from the host
dependency context. Microsoft's
[C# language-version policy](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)
maps .NET 10 to C# 14 and warns against selecting a language version newer than the target framework.

Executable tests cover collection expressions, raw string literals, property patterns, nullable
directives, awaited code, and representative `@using`, `@model`, `@inject`, and `@functions`
directives. Passing those tests defines the currently supported syntax; it is not a promise that
every future Razor grammar feature will work through the Razor 6 parser.

## Lifecycle and exit criteria

Keep the two Razor packages updated as a single compatibility group, audit their transitive graph,
and run compiler/generation/rendering regressions for every change. A target-framework upgrade must
also update the explicit C# language selection and modern-syntax tests.

Replace this adapter only when one of these conditions is met:

1. Microsoft ships a supported, redistributable in-process compiler API for arbitrary Razor input.
2. RazorLight deliberately adopts build-time/precompiled-only execution and removes runtime source
   compilation from the applicable product surface.
3. The project approves ownership of a vendored compiler, including servicing, licensing notices,
   compatibility baselines, and security response.

Until then, a new Razor feature request that fails in the compatibility parser is tracked as new
work and is not addressed by loading SDK-private binaries.
