---
id: DECISION-003
title: Establish independent Dijgrid package identity and release controls
status: accepted
date: 2026-08-06
author: justin
---

Publish the independently maintained RazorLight line under package IDs owned by the `dijgrid`
NuGet.org account, with a new major version and protected, keyless release automation.

## Context

The existing `RazorLight` package is owned by `toddams` and `RazorLightTeam`; the existing
`RazorLight.Precompile` package is owned by `johnzabroski` and `RazorLightTeam`. The independent
maintainer does not control `RazorLightTeam`, so retaining either historical package ID would make
the release process depend on an unverified ownership transfer.

The maintained projects target .NET 10 only and have intentionally diverged from the historical
`2.3.1` package. Treating the independent release as another `2.x` patch would misrepresent that
framework and compatibility break.

## Decision

- Publish the library as `Dijgrid.RazorLight`, optional HTML integration as
  `Dijgrid.RazorLight.Html`, and tool as `Dijgrid.RazorLight.Precompile`.
- Keep the existing `RazorLight` and `RazorLight.Precompile` CLR namespaces and command name so the
  package identity change does not create an unrelated source-compatibility break.
- Begin the independent release line with `3.0.0-beta.1`; release tags use exact SemVer
  `v<major>.<minor>.<patch>[-prerelease]` form and supply the package version to the build.
- Publish public packages to NuGet.org and attach the exact reviewed `.nupkg` and `.snupkg`
  artifacts to a GitHub Release for the same tag.
- The NuGet.org trusted-publishing policy is owned by the `dijgrid` account and trusts only the
  `dijgrid/razor-light` repository, `release.yml` workflow, and `nuget` GitHub environment.
- The `nuget` environment requires approval from the `dijgrid` GitHub user. Release automation uses
  GitHub OIDC to obtain a short-lived NuGet credential; no long-lived NuGet API key is stored.
- Do not deprecate or modify the historical packages because this maintainer does not own them.
  Documentation must identify them as upstream artifacts and direct independent-line consumers to
  the new package IDs.

## Consequences

Positive:

- Package publication no longer depends on historical owners.
- The package ID and major version make the independent maintenance and compatibility break clear.
- Protected-environment approval and short-lived credentials reduce release supply-chain risk.
- GitHub and NuGet releases share one reviewed artifact set and one explicit version.

Negative:

- Existing consumers must change package references even though CLR namespaces remain stable.
- NuGet download history and dependent-package discovery do not transfer to the new IDs.
- The first release requires one-time NuGet.org trusted-publishing setup by the `dijgrid` account.
