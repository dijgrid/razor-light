# Changelog

Notable changes to this independently maintained continuation are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Package identity and
versioning follow the independent release policy in [`docs/releasing.md`](docs/releasing.md).

## Unreleased

## 3.0.0 - 2026-08-07

### Added

- Independent-maintenance provenance and upstream synchronization policy.
- Repository contribution, support, security, conduct, and task-management guidance.
- PlanFS backlog for modernization work.
- Cross-platform .NET 10 test coverage, package validation, and maintained sample checks.
- Compatibility, framework support, dependency policy, testing, and code-quality documentation.
- Independent `Dijgrid.RazorLight` and `Dijgrid.RazorLight.Precompile` package identities beginning
  at version `3.0.0`.
- Deterministic-output, package-content, symbol, and Source Link validation for both release
  artifacts.
- SDK package compatibility validation against the inherited `RazorLight 2.3.1` baseline, with
  reviewed suppressions for its retired framework groups and a human-readable public API inventory.
- Protected tag-triggered release automation using GitHub OIDC and NuGet trusted publishing.

### Changed

- Repository metadata and links now point to `dijgrid/razor-light`.
- GitHub automation and dependency maintenance use current, least-privilege workflows.
- Maintained projects and samples now target .NET 10.
- Dependencies are centrally managed, use HTTPS-only restore sources, and have no known NuGet
  advisories in the recorded audit.
- README guidance and package metadata now describe the maintained framework and hosting baseline.
- Package metadata identifies Dijgrid as the independent maintainer while preserving upstream
  provenance and existing CLR namespaces.

### Removed

- Obsolete PAT-based pull request rebasing and direct NuGet publishing automation.
- Abandoned command-line and globbing dependencies, obsolete runtime branches, and stale .NET Core
  2.x/3.x deployment guidance.
