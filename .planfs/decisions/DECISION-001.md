---
id: DECISION-001
title: Maintain RazorLight as an independent continuation
status: accepted
date: 2026-08-06
author: justin
---

Maintain this repository as an independent continuation rather than a contribution fork of the
abandoned upstream project.

## Context

The upstream repository has been inactive for years, while this codebase still has active users and
needs substantial framework, dependency, security, and maintenance changes. Remaining in the fork
network would imply an upstream contribution relationship that no longer matches the intended
roadmap.

## Decision

Evolve `dijgrid/razor-light` on its own roadmap while preserving the complete Git history, Apache
2.0 license, upstream attribution, and an `upstream` fetch remote.

Upstream changes may be reviewed and cherry-picked intentionally. They are not merged or synchronized
automatically.

## Consequences

Positive:

- The repository can establish independent package, compatibility, and release policies.
- GitHub issues, security reporting, and automation belong to the active project.
- Original history and authorship remain intact.

Negative:

- Compatibility with upstream is no longer automatic.
- The project must clearly distinguish inherited releases from independently maintained releases.
- Maintainers own security and release decisions that were previously deferred upstream.
