# Code quality baseline

The .NET SDK analyzers run at the latest level selected by the pinned SDK. CI treats compiler and
analyzer warnings as errors, while `.editorconfig` remains the source of truth for repository
formatting. This establishes an automated baseline without rewriting unrelated historical code.

## Warning suppressions

- The library, precompile tool, sandbox, samples, and both test projects have no project-wide
  compiler warning suppressions.
- One main-suite test uses a local `CS0618` pragma because its purpose is to preserve compatibility
  coverage for an obsolete public constructor. The suppression surrounds only that constructor call.

## Nullable reference types

Nullable reference types are enabled without suppressions in `RazorLight.Precompile.Tests`. They are
not yet enabled in the public library, precompile tool, sample, sandbox, or legacy xUnit suite.
Enabling them for the public library is a
compatibility-sensitive change because annotations affect consumer diagnostics and the recorded
public API surface. TASK-009 stages adoption project by project, with the library last and guarded by
the API baseline.

## Runtime cleanup

Maintained production projects target only .NET 10. Obsolete `Assembly.CodeBase` and
`AssemblyName.EscapedCodeBase` paths have been replaced by `Assembly.Location` and assembly identity.
Unreachable .NET Standard and older .NET runtime branches were removed from production and sample
code. The public `LegacyFixAssemblyPathFormatter` type remains for binary/source compatibility, but
now uses supported location behavior.
