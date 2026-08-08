---
id: TASK-029
title: Separate supported extension points from runtime internals
status: todo
priority: high
epic: EPIC-modernization
milestone: MILESTONE-library-quality
dependsOn:
  - TASK-018
  - TASK-021
  - TASK-027
  - TASK-028
tags:
  - api
  - internals
  - extensibility
  - beta
createdAt: 2026-08-08T04:17:57.601Z
updatedAt: 2026-08-08T04:17:57.601Z
refinementState: ready
---

Separate supported user extension points and the generated-template ABI from compiler, buffering,
activation, and orchestration implementation details before the first beta.

## Acceptance criteria

- [ ] Public types are assigned to one documented tier: application API, supported extension point,
      generated-template ABI, or implementation detail.
- [ ] Custom `RazorLightProject`/`RazorLightProjectItem`, output encoder, and cache-provider
      scenarios retain narrow supported contracts with end-to-end tests.
- [ ] `IEngineHandler`, compiler orchestration, source-generator/instrumentation, property injector,
      factory-provider, cache-record, and `RazorLight.Internal.Buffering` types become internal where
      they are not required by a supported extension or generated template.
- [ ] Generated templates use an explicit, separately reviewed ABI consisting only of the page base,
      page/context contracts, helper/content types, and template metadata they actually reference.
- [ ] Public concrete implementations are sealed or hidden unless inheritance is an intentional and
      tested extension mechanism.
- [ ] Dependency-injection registration no longer makes internal services appear to be supported
      consumer services.
- [ ] Public API and generated-code snapshots prove that removals do not break runtime compilation or
      precompiled artifacts produced by the same 3.x release.

## Baseline

The post-TASK-019 public API record contains 657 entries. Of those, 108 mention the
`RazorLight.Internal` namespace, while another 148 mention compiler generation or instrumentation
namespaces. Repository searches show the latter are primarily exercised by RazorLight's own tests;
indexed GitHub results for compiler interfaces are dominated by forks or vendored source copies.
