---
id: DECISION-005
title: Define the supported RazorLight 3.x API layers
status: accepted
date: 2026-08-08
author: justin
---

Define separate application, extension, generated-template, and implementation API layers before
publishing the first 3.0 beta. Do not preserve inherited public surface merely because implementation
details were historically exported.

## Context

The post-TASK-019 API record contains 657 entries. It mixes ordinary rendering operations with
mutable options, handler/compiler object graphs, Razor compiler passes, cache records, activation
services, generated-page requirements, and 108 entries whose namespace itself says
`RazorLight.Internal`.

Three public engine overloads are obsolete with `error: true` and throw `NotImplementedException`.
The obsolete `EngineFactory` types, the .NET Framework assembly-path workaround, and a second
file-system engine factory have supported builder replacements. `IRazorLightEngine.Options` permits
post-build mutation, while `IRazorLightEngine.Handler` exposes compiler and page-factory internals
mostly so applications can invalidate cache entries.

Repository documentation and indexed public code show that cache invalidation and custom project
sources are real consumer scenarios. By contrast, indexed uses of compiler orchestration interfaces
are dominated by RazorLight forks or copied source trees. The cleanup must preserve capabilities,
not accidental access paths.

## Decision

Classify the 3.x surface into four layers:

1. The application API contains the engine abstraction, builder/DI entry points, rendering methods,
   cache administration, generic text/content contracts, and documented exceptions. Applications do
   not receive mutable runtime options or the internal handler graph.
2. Supported extension points are deliberately narrow and tested end to end: custom projects and
   project items, output encoders, cache providers, and page initialization/service resolution.
   Extension interfaces expose only behavior every supported implementation can provide.
3. The generated-template ABI contains the page bases, context/content/helper contracts, generated
   write methods, injection metadata, and template identity required by generated C#. It remains
   public only where generated assemblies require accessibility and is tracked separately from the
   normal application API.
4. Compiler wiring, Razor passes, handler/factory orchestration, activation implementations, cache
   records, and buffering/pooling types are implementation details. They become internal unless a
   characterized extension or generated-code requirement proves otherwise.

Additional policy:

- Remove compile-time-error obsolete members and unsupported stubs in 3.0 instead of carrying traps.
- Snapshot configuration when an engine is built or resolved. Do not support mutation through an
  engine instance.
- Replace `engine.Handler.Cache` with a narrow engine-level cache administration contract; keep the
  storage/provider contract separate.
- Keep compilation and caches singleton under DI, create one scope per top-level render, and share
  that scope with layouts and includes.
- Add no new synchronous wrappers around asynchronous work. Existing synchronous generated-template
  methods remain only where the Razor 6 generated ABI requires them; async authoring APIs remain the
  preferred surface.
- Public API analyzer and package-validation diffs must enumerate every removal. Generated-code
  compatibility receives focused snapshots and compile/render tests rather than being hidden in the
  application baseline.
- Beta releases may make further documented breaks before the first release candidate. The release
  candidate freezes the intended 3.0 surface; stable 3.x follows semantic versioning.

## Consequences

Positive:

- The beta teaches one coherent application API instead of exposing the dependency graph.
- Real extension scenarios remain supported and testable.
- Internal compiler and buffering changes stop creating unnecessary consumer compatibility costs.
- Precompiled templates have an explicit ABI to validate in TASK-022.

Negative:

- Consumers using handlers, mutable options, compiler internals, or inherited factories must migrate.
- Precompiled artifacts must be regenerated for 3.0; cross-version generated-ABI compatibility is
  not claimed until executable tests establish it.
- Several focused implementation changes must land before `3.0.0-beta.1` can be published.
