---
id: TASK-032
title: Confine file and cache paths to configured roots
status: todo
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-015
tags:
  - security
  - filesystem
  - caching
  - pre-release
createdAt: 2026-08-08T16:19:51.437Z
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Prevent template, source, precompile, and cache keys from resolving outside their configured roots.

## Acceptance criteria

- [ ] File-system template and C# source keys are resolved through one canonical containment helper.
- [ ] Containment uses full paths, platform-correct case comparison, and rejects rooted or traversing
      keys that escape the configured root.
- [ ] Simple and hashed file-cache strategies cannot read, write, or delete outside the cache root.
- [ ] The precompile CLI rejects template inputs outside an explicitly supplied base directory.
- [ ] Extension and prefix matching use explicit ordinal semantics consistent with documented keys.
- [ ] Tests cover `..`, absolute paths, sibling-prefix paths, mixed separators, platform casing, and
      valid nested templates and sources.
- [ ] Symlink/reparse-point behavior is either enforced or explicitly documented as part of the
      trusted-project boundary.

## Baseline findings

`FileSystemRazorProject` combines template keys with its root without checking the resulting full
path. A repository probe confirmed that `../Embedded/Empty.cshtml` escapes an `Assets/Files` root
and resolves an existing file. The simple disk-cache strategy also combines caller keys directly
with the cache directory.
