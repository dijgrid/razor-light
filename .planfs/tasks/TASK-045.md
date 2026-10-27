---
id: TASK-045
title: Replace the legacy Source Link validation CLI with maintained tooling
status: todo
priority: high
createdAt: 2026-10-27T19:37:30.658Z
updatedAt: 2026-10-27T19:37:30.658Z
---

The latest stable `sourcelink` CLI (3.1.1) bundles Newtonsoft.Json 10.0.3 and declares
System.Text.RegularExpressions 4.3.0. NuGet's advisory feed reports high-severity matches
GHSA-5crp-9r3c-p9vr and GHSA-cmhx-cq75-c4mj. The regex DLL is not bundled and its actual runtime
implementation comes from the selected shared framework; this distinction needs to be retained.
Central project package versions cannot update a tool's bundled DLLs, and no newer version of this
CLI package exists. Do not remove validation coverage or suppress findings to make an audit pass.

## Acceptance criteria

- [ ] Select maintained tooling or implement a repository-owned validator on the supported .NET SDK.
- [ ] Preserve PDB Source Link metadata inspection, immutable source URLs and document hash validation.
- [ ] Update the tool manifest, validation scripts, documentation and CI consistently.
- [ ] Audit the replacement's direct and bundled dependencies and validate all package checks.

## Implementation notes

Discovered during TASK-043 by auditing all 66 entries of the CLI dependency manifest against the
NuGet vulnerability feed using NuGet's version-range matcher. This does not affect the clean
RazorLight consumer package vulnerability audit. Detailed evidence is in the upgrade report.
