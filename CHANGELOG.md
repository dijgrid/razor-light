# Changelog

Notable changes to this independently maintained continuation are recorded here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Versioning and package
identity will be finalized before the first independent release.

## Unreleased

### Added

- Independent-maintenance provenance and upstream synchronization policy.
- Repository contribution, support, security, conduct, and task-management guidance.
- PlanFS backlog for modernization work.
- Cross-platform .NET 10 test coverage, package validation, and maintained sample checks.
- Compatibility, framework support, dependency policy, testing, and code-quality documentation.

### Changed

- Repository metadata and links now point to `dijgrid/razor-light`.
- GitHub automation and dependency maintenance use current, least-privilege workflows.
- Maintained projects and samples now target .NET 10.
- Dependencies are centrally managed, use HTTPS-only restore sources, and have no known NuGet
  advisories in the recorded audit.
- README guidance and package metadata now describe the maintained framework and hosting baseline.

### Removed

- Obsolete PAT-based pull request rebasing and direct NuGet publishing automation.
- Abandoned command-line and globbing dependencies, obsolete runtime branches, and stale .NET Core
  2.x/3.x deployment guidance.
