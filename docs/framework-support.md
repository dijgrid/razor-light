# Framework support policy

RazorLight's independently maintained line targets `net10.0`, the current .NET LTS baseline. The
library, precompile tool, test projects, sandbox, and maintained samples use the same target so CI
exercises the runtime configuration consumers are expected to use.

## Why `netstandard2.0` was removed

The inherited `netstandard2.0` asset depended on the ASP.NET Core 2.1 Razor stack. RazorLight uses
Razor compiler internals, so that asset could not be retained as a thin compatibility facade: it
required a separate, end-of-life compiler and dependency graph. Carrying it would conflict with the
project's secure-dependency policy and multiply the behavior matrix without a supported runtime on
which to validate it.

The same policy removes the inherited .NET Core 2.x/3.1, .NET 5, .NET 6, and sample-only .NET 7
targets. Additional current targets may be added later when they provide demonstrated consumer value
and can share the same secure Razor behavior baseline.

## Consumer migration

This framework change belongs to the next major RazorLight release.

- Applications must target .NET 10 or a later compatible runtime before adopting that release.
- Rebuild extensions and custom `RazorLightProject` implementations for `net10.0` and rerun their
  template compilation tests.
- Do not assume generated Razor output or diagnostic text is byte-for-byte identical across the old
  and new compiler stacks; validate representative templates using the migration policy in
  `compatibility-baseline.md`.
- Consumers that cannot move to .NET 10 must remain on the inherited `2.3.1` package line. That line
  is preserved for provenance but is not an actively supported or security-maintained branch here.

The repository's `global.json` is the authoritative SDK selection. CI installs only that supported
SDK and runs restore, build, and tests on Windows, Linux, and macOS.

## Deployment modes

Runtime compilation is tested in framework-dependent, self-contained, and extraction-based
single-file applications on Windows and Linux. Trimming and Native AOT are explicitly unsupported
for runtime compilation and produce analyzer diagnostics at consumer call sites. See the
[deployment compatibility matrix](deployment.md) for the required project properties, analyzer
contract, and precompiled alternatives.

## Compiler lifecycle

The .NET target and generated-C# language version move together. The `net10.0` line intentionally
compiles generated templates as C# 14 and tests modern C# syntax in Razor templates. A future target
framework change must select that framework's supported C# version explicitly and update the syntax
matrix; a host application's dependency-context value does not control this policy.

Runtime Razor parsing currently uses a version-guarded Razor 6 compatibility adapter because the
.NET 10 Razor SDK is a build-time tool and the .NET 10 MVC runtime-compilation API is obsolete. See
the [Razor compiler integration decision](razor-compiler-integration.md) for the evaluated options,
risks, sources, and replacement criteria. This boundary supports the tested syntax matrix but does
not claim support for every Razor feature introduced after Razor 6.
