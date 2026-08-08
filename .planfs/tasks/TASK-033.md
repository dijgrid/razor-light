---
id: TASK-033
title: Make render cancellation resource-safe
status: todo
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
updatedAt: 2026-08-08T16:20:27.633Z
milestone: MILESTONE-library-quality
refinementState: ready
---

Ensure cancellation never abandons render work that still owns pooled buffers, writers, or scoped
services.

## Acceptance criteria

- [ ] Template execution, includes, sections, layouts, and final output do not return while
      uncancelled underlying work can still access disposed render resources.
- [ ] Render cancellation remains cooperative: templates receive the token and safe boundaries
      observe it without pretending synchronous or token-ignorant code was stopped.
- [ ] Token-aware writer APIs are used where available; otherwise RazorLight awaits owned writes
      before releasing buffers.
- [ ] An explicit include token reaches child lookup and rendering rather than cancelling only the
      caller's wait around a parent-token operation.
- [ ] The parameterless `FlushAsync`, include, and section helpers consistently use the active page
      cancellation token.
- [ ] Tests cover a token-aware template, a token-ignoring asynchronous template, delayed sections
      and includes, writer cancellation, and DI-scope/buffer lifetime safety.
- [ ] Cancellation documentation precisely distinguishes shared compilation wait cancellation from
      owned render-operation cancellation.

## Baseline findings

The renderer currently applies `Task.WaitAsync(token)` to `ExecuteAsync`, includes, sections, and
buffer output. If the underlying operation ignores the token, RazorLight can return and dispose its
buffer pool and DI scope while that operation continues writing.
