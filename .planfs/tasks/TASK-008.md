---
id: TASK-008
title: Define package identity and release automation
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-release-readiness
dependsOn:
  - TASK-004
  - TASK-005
  - TASK-007
tags:
  - packaging
  - release
  - nuget
  - supply-chain
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-07T06:13:02.341Z
refinementState: ready
---

Decide how the independent continuation is named and versioned, then implement a controlled,
reproducible release process.

## Implementation readiness

Maintainer decisions are recorded in DECISION-003. Repository automation, GitHub environment
protection, and the documented trusted-publishing policy in the `dijgrid` NuGet.org account are
active.

## Maintainer decisions required

Please record answers in this task before moving it to `in_progress`:

1. Can the maintainer account or organization publish both `RazorLight` and
   `RazorLight.Precompile` on NuGet.org? Record the owner names or the outcome of an ownership
   transfer request; do not record credentials.
2. If those IDs are unavailable, should the independent packages use
   `Dijgrid.RazorLight` / `Dijgrid.RazorLight.Precompile`, or another prefix? The recommended
   fallback is the `Dijgrid.*` pair because it makes ownership unambiguous while allowing the
   existing CLR namespaces to remain compatible.
3. Should the independent line begin at `3.0.0`? This is recommended because the maintained package
   is .NET 10-only and therefore cannot be a compatible patch to upstream `2.3.1`.
4. Is NuGet.org the public release destination, and should GitHub Releases receive the reviewed
   package artifacts at the same tag?
5. Which NuGet.org user or organization should own the trusted-publishing policy, and who should be
   the required reviewer for a protected GitHub `nuget` environment?

NuGet.org supports GitHub Actions OIDC through
[trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). Prefer that
over a long-lived API key when it is available for the selected owner.

## Recorded maintainer decisions

DECISION-003 records the approved release identity and controls:

1. The maintainer does not control the historical `RazorLightTeam` NuGet.org owner, so the existing
   package IDs will not be retained.
2. The independent packages are `Dijgrid.RazorLight`, `Dijgrid.RazorLight.Html`, and
   `Dijgrid.RazorLight.Precompile`; existing CLR namespaces and the tool command remain unchanged.
3. The independent version line begins with `3.0.0-beta.1` and uses exact SemVer
   `v<major>.<minor>.<patch>[-prerelease]` release tags.
4. NuGet.org is the public destination, and the same reviewed artifacts are attached to the matching
   GitHub Release.
5. The NuGet.org trusted-publishing owner is the `dijgrid` account. The policy is restricted to
   `dijgrid/razor-light`, `release.yml`, and the protected `nuget` GitHub environment; the `dijgrid`
   GitHub user is the required reviewer.

## Implementation plan

1. Capture the approved package names, CLR namespace policy, first version, ownership, and
   deprecation approach in a PlanFS decision record.
2. Centralize package metadata and version inputs, including independent-maintainer and upstream
   provenance links.
3. Produce deterministic `.nupkg` and `.snupkg` artifacts in CI and retain them for review without
   publishing.
4. Add a tag-triggered release workflow with a protected `nuget` environment, least-privilege
   permissions, and OIDC trusted publishing where available.
5. Exercise the workflow without publishing, inspect all package artifacts, and document the
   first-release checklist and rollback/yank procedure.

## Scope boundaries

- This task establishes identity, artifact production, and release controls.
- API compatibility enforcement against the selected baseline belongs to TASK-016.
- No package may be pushed as part of implementing or testing this task.

## Acceptance criteria

- [x] Ownership of the existing NuGet IDs is verified before retaining them.
- [x] Package identity, namespace compatibility, versioning, and deprecation policy are documented.
- [x] Package metadata clearly identifies the independent maintainer and upstream provenance.
- [x] Package contents, symbols, deterministic build, and Source Link are validated in CI.
- [x] Publishing requires an explicit version/tag, a protected GitHub environment, and maintainer
      approval.
- [x] NuGet credentials are scoped minimally and supplied only through GitHub secrets or trusted
      publishing.
- [x] A dry-run or package-artifact review occurs before the first independent release.

## Implementation notes

Do not assume access to the existing `RazorLight` and `RazorLight.Precompile` package IDs. If ownership
cannot be transferred or verified, choose distinct IDs while preserving namespaces only where legally
and technically appropriate.

- Verified that the historical packages are owned by `toddams` / `RazorLightTeam` and
  `johnzabroski` / `RazorLightTeam`; the maintainer confirmed those owners are not controlled here.
- Centralized version `3.0.0-beta.1`, independent-maintainer metadata, provenance, license/readme,
  portable symbols, and Source Link for the Core, optional HTML, and precompile packages.
- Added repeat-build hash checks and package/symbol validation scripts, then integrated them into CI
  with reviewable artifacts for all three packages.
- Added a SemVer tag-triggered release workflow that rebuilds and verifies artifacts before waiting
  for the protected `nuget` environment, exchanges GitHub OIDC for a short-lived NuGet credential,
  and creates a matching GitHub Release only after all packages publish.
- Configured the repository's `nuget` environment with `dijgrid` as required reviewer and a
  `v*.*.*` tag deployment policy. No packages, tags, or releases were published.
- The maintainer confirmed on 2026-08-07 that the matching trusted-publishing policy was created in
  the `dijgrid` NuGet.org account.
- Built and inspected all six `3.0.0-beta.1` artifacts locally, validated their contents and Source
  Link metadata, restored Core and optional HTML packages in a clean consumer, and installed the tool
  from the local package source.
- Documented package review, first-release, trusted-publishing, and rollback/unlisting procedures in
  `docs/releasing.md`.
