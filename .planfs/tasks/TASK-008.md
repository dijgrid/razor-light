---
id: TASK-008
title: Define package identity and release automation
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-modernization-foundation
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
updatedAt: 2026-08-06T00:00:00Z
---

Decide how the independent continuation is named and versioned, then implement a controlled,
reproducible release process.

## Acceptance criteria

- [ ] Ownership of the existing NuGet IDs is verified before retaining them.
- [ ] Package identity, namespace compatibility, versioning, and deprecation policy are documented.
- [ ] Package metadata clearly identifies the independent maintainer and upstream provenance.
- [ ] Package contents, symbols, deterministic build, and Source Link are validated in CI.
- [ ] Publishing requires an explicit version/tag, a protected GitHub environment, and maintainer
      approval.
- [ ] NuGet credentials are scoped minimally and supplied only through GitHub secrets or trusted
      publishing.
- [ ] A dry-run or package-artifact review occurs before the first independent release.

## Implementation notes

Do not assume access to the existing `RazorLight` and `RazorLight.Precompile` package IDs. If ownership
cannot be transferred or verified, choose distinct IDs while preserving namespaces only where legally
and technically appropriate.
