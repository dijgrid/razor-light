# Code quality baseline

The .NET SDK analyzers run at the latest level selected by the pinned SDK. CI treats compiler and
analyzer warnings as errors, while `.editorconfig` remains the source of truth for repository
formatting. This establishes an automated baseline without rewriting unrelated historical code.

## Warning suppressions

- The library, precompile tool, sandbox, samples, and both test projects have no project-wide
  compiler warning suppressions.
- The maintained test suites do not use project-wide or local compiler-warning suppressions.

## Nullable reference types

Nullable reference types are enabled without suppressions in `RazorLight.Precompile.Tests` and the
Azure Functions sample. They are not yet enabled in the public library, precompile tool, Entity
Framework sample, sandbox, or legacy xUnit suite. TASK-009 covers the non-public projects. Public
library annotations are compatibility-sensitive because they affect consumer diagnostics and the
recorded API surface, so TASK-024 handles them separately after package/API validation is available.

## Runtime cleanup

Maintained production projects target only .NET 10. Obsolete `Assembly.CodeBase` and
`AssemblyName.EscapedCodeBase` paths have been replaced by `Assembly.Location` and assembly identity.
Unreachable .NET Standard and older .NET runtime branches were removed from production and sample
code. The .NET Framework-only assembly path formatter and registration hook were removed before the
3.0 beta; the maintained line uses `Assembly.Location` on .NET 10.
