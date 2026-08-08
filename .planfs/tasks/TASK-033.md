---
id: TASK-033
title: Make render cancellation resource-safe
status: done
priority: high
epic: EPIC-modernization
dependsOn:
  - TASK-013
tags:
  - cancellation
  - rendering
  - reliability
  - pre-release
createdAt: 2026-08-08T16:19:52.703Z
updatedAt: 2026-08-08T16:41:01.604Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Ensure cancellation never abandons render work that still owns pooled buffers, writers, or scoped
services.

## Acceptance criteria

- [x] Template execution, includes, sections, layouts, and final output do not return while
      uncancelled underlying work can still access disposed render resources.
- [x] Render cancellation remains cooperative: templates receive the token and safe boundaries
      observe it without pretending synchronous or token-ignorant code was stopped.
- [x] Token-aware writer APIs are used where available; otherwise RazorLight awaits owned writes
      before releasing buffers.
- [x] An explicit include token reaches child lookup and rendering rather than cancelling only the
      caller's wait around a parent-token operation.
- [x] The parameterless `FlushAsync`, include, and section helpers consistently use the active page
      cancellation token.
- [x] Tests cover a token-aware template, a token-ignoring asynchronous template, delayed sections
      and includes, writer cancellation, and DI-scope/buffer lifetime safety.
- [x] Cancellation documentation precisely distinguishes shared compilation wait cancellation from
      owned render-operation cancellation.

## Baseline findings

The renderer currently applies `Task.WaitAsync(token)` to `ExecuteAsync`, includes, sections, and
buffer output. If the underlying operation ignores the token, RazorLight can return and dispose its
buffer pool and DI scope while that operation continues writing.

## Implementation notes

- Removed wait-abandonment from page, section, include, layout, and final-buffer operations. Owned
  render work now completes before cancellation is observed at a safe resource boundary.
- Made the include callback token-aware so an explicit token controls both child compilation and
  child rendering, and made parameterless flush use the active page token.
- Added token-aware buffered-writer paths through the final output writer.
- Added deterministic tests proving token-ignoring work retains live buffers and scoped services
  until it completes, while cooperative templates and writers still cancel promptly.

## Verification

- `dotnet test tests/RazorLight.Tests/RazorLight.Tests.csproj --framework net10.0 --configuration Release --no-restore` (297 passed)
- `dotnet test tests/RazorLight.Precompile.Tests/RazorLight.Precompile.Tests.csproj --configuration Release --no-restore` (128 passed)
- `dotnet build RazorLight.sln --configuration Release --no-restore`
- `git diff --check`
