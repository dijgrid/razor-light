# Changelog

Notable changes to this independently maintained continuation are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Package identity and
versioning follow the independent release policy in [`docs/releasing.md`](docs/releasing.md).

## Unreleased

## 3.0.0-beta.3 - 2026-08-08

This publishable beta supersedes the earlier prerelease tag attempts, which did not publish NuGet
packages. It contains the same 3.0 API and functionality described below, plus release-workflow
validation corrections.

### Fixed

- The protected publish job now revalidates the reviewed package artifacts without assuming local
  build output is present, while retaining package, symbol, Source Link, and SHA-256 checks.
- Release documentation and package examples now use the publishable `3.0.0-beta.3` version.

## 3.0.0-beta.1 - 2026-08-08

This first independent beta requires .NET 10, changes the package ID to `Dijgrid.RazorLight`, uses
plain-text output by default, moves HTML encoding into the optional `Dijgrid.RazorLight.Html` package,
removes MVC tag helpers, and intentionally retires obsolete public APIs. Read the
[2.3.1-to-3.0 migration guide](docs/migration-3.0.md) before upgrading.

### Added

- Added cross-platform line and branch coverage ratchets plus a reproducible BenchmarkDotNet suite
  for compilation, rendering, caching, concurrency, dependency injection, and lifecycle scenarios.
- A supported precompiled-only engine entry point renders deterministic build artifacts without
  constructing the Razor/Roslyn compiler graph or silently falling back to runtime compilation.
  Template artifacts now carry version, model-contract, and source-checksum metadata.
- Templates can compose ordinary trusted C# source files through global `AddCSharpSource` builder
  configuration or a per-template `@compileSource` directive. Sources compile as separate syntax
  trees with mapped diagnostics and participate in project change-token invalidation.

### Changed

- DI-created engines now create one service scope per top-level render and share it across pages,
  layouts, and includes. `@inject` uses that scope, `AddPageInitializer` replaces the old pre-render
  callback spelling, and missing ViewBag members now return `null` without suppressing other dynamic
  binding errors.
- `RazorLightEngineBuilder.Build()` now returns `IRazorLightEngine`. The engine exposes direct
  `IsTemplateCached` and `InvalidateTemplate` operations instead of public handler/options graphs,
  and builder configuration is copied when the engine is built.
- Removed inherited obsolete/error-only factories and render overloads, the retired .NET Framework
  assembly-path workaround, and the redundant file-system engine factory before the 3.0 beta API
  becomes a compatibility commitment. Supported replacements use `RazorLightEngineBuilder`, generic
  rendering methods, and structured compilation diagnostics.
- RazorLight is now a generic C# text-template engine: core expression output is plain text, HTML
  encoding is opt-in through `Dijgrid.RazorLight.Html`, and the core package no longer requires the
  ASP.NET Core shared framework.
- ASP.NET-owned content, tag-helper, and compiled-item contracts have been replaced by RazorLight
  content and output-encoder abstractions. This is an intentional next-major API and behavior break.
- The `RazorLight` public API now publishes nullable reference annotations. Runtime behavior is
  unchanged, but nullable-enabled consumers may receive new diagnostics; see
  [`docs/nullability.md`](docs/nullability.md).
- String templates now receive the same built-in imports and `AddDefaultNamespaces` values as
  project-backed templates.
- Generic rendering without `@model` remains dynamic. New explicit model-type overloads support
  strongly typed compilation, including LINQ lambda expressions, when templates cannot declare
  `@model` themselves.
- Reusing a string-template key with changed content, selected model type, or configured imports
  replaces the active compilation across the compiler and configured template cache.
- Compiler descriptors and configured page factories now share one invalidation contract. Explicit
  removal, replacement, project change tokens, and failed-compilation retries cannot return or
  permanently retain stale compiled templates; precompiled providers support runtime cache mutation.
- Generated template code now uses an explicit C# 14 parse policy instead of inheriting the host
  dependency context's language version. Modern C# and Razor directive baselines guard the retained,
  version-checked Razor 6 compatibility adapter.
- Runtime compilation now declares trimming and dynamic-code requirements so consumers receive
  `IL2026` and `IL3050` diagnostics for unsupported trimmed and Native AOT deployments.
- Runtime-compiled templates now emit portable PDBs on every platform, avoiding the legacy native
  Windows PDB writer and its version-sensitive failures in single-file deployments.
- Metadata discovery skips dynamic assemblies, empty assembly locations, and missing reference
  files, with actionable configuration guidance when no usable compilation references remain.
- Metadata discovery now defaults to project assemblies and RazorLight's required runtime closure
  instead of every host package. Exact include and exclude controls and an explicit broad-discovery
  compatibility mode make template dependencies intentional.
- Production compiler and Razor diagnostics now redact template-derived messages, mapped paths, and
  missing-template key inventories by default. `EnableDebugMode` explicitly restores full details.
- The documented trust boundary treats in-process Razor templates as executable trusted code and
  requires external process isolation for untrusted template authors.
- Windows and Linux CI now executes framework-dependent, self-contained, and extraction-based
  single-file deployment probes and guards the documented trim-warning inventory.

### Repository and release infrastructure

- Independent-maintenance provenance and upstream synchronization policy.
- Repository contribution, support, security, conduct, and task-management guidance.
- PlanFS backlog for modernization work.
- Cross-platform .NET 10 test coverage, package validation, and maintained sample checks.
- Compatibility, framework support, dependency policy, testing, and code-quality documentation.
- Independent `Dijgrid.RazorLight`, `Dijgrid.RazorLight.Html`, and
  `Dijgrid.RazorLight.Precompile` package identities beginning at version `3.0.0-beta.1`.
- Deterministic-output, package-content, symbol, and Source Link validation for all three release
  artifacts.
- SDK package compatibility validation against the inherited `RazorLight 2.3.1` baseline, with
  reviewed suppressions for its retired framework groups and a human-readable public API inventory.
- Protected tag-triggered release automation using GitHub OIDC and NuGet trusted publishing.

### Repository infrastructure changes

- Repository metadata and links now point to `dijgrid/razor-light`.
- GitHub automation and dependency maintenance use current, least-privilege workflows.
- Maintained projects and samples now target .NET 10.
- Dependencies are centrally managed, use HTTPS-only restore sources, and have no known NuGet
  advisories in the recorded audit.
- README guidance and package metadata now describe the maintained framework and hosting baseline.
- Package metadata identifies Dijgrid as the independent maintainer while preserving upstream
  provenance and existing CLR namespaces.

### Repository cleanup

- Obsolete PAT-based pull request rebasing and direct NuGet publishing automation.
- Abandoned command-line and globbing dependencies, obsolete runtime branches, and stale .NET Core
  2.x/3.x deployment guidance.
