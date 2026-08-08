---
id: TASK-032
title: Confine file and cache paths to configured roots
status: done
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
updatedAt: 2026-08-08T16:34:23.986Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Prevent template, source, precompile, and cache keys from resolving outside their configured roots.

## Acceptance criteria

- [x] File-system template and C# source keys are resolved through one canonical containment helper.
- [x] Containment uses full paths, platform-correct case comparison, and rejects rooted or traversing
      keys that escape the configured root.
- [x] Simple and hashed file-cache strategies cannot read, write, or delete outside the cache root.
- [x] The precompile CLI rejects template inputs outside an explicitly supplied base directory.
- [x] Extension and prefix matching use explicit ordinal semantics consistent with documented keys.
- [x] Tests cover `..`, absolute paths, sibling-prefix paths, mixed separators, platform casing, and
      valid nested templates and sources.
- [x] Symlink/reparse-point behavior is either enforced or explicitly documented as part of the
      trusted-project boundary.

## Baseline findings

`FileSystemRazorProject` combines template keys with its root without checking the resulting full
path. A repository probe confirmed that `../Embedded/Empty.cshtml` escapes an `Assets/Files` root
and resolves an existing file. The simple disk-cache strategy also combines caller keys directly
with the cache directory.

## Implementation notes

- Added a shared canonical containment helper with Windows-insensitive and Unix-sensitive path
  comparison, and routed file-project templates, C# sources, disk caches, and explicit-base CLI
  inputs through it.
- Preserved the public project-root and CLI output-path contracts while using canonical absolute
  roots internally for file operations.
- The hash strategy remains confined because template keys are reduced to a fixed hexadecimal
  filename; the simple strategy now validates every generated assembly and symbol path.
- Documented symbolic-link and reparse-point behavior as part of the trusted-project boundary.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (291 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release` (128 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore`
- `git diff --check`
