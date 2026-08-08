---
id: TASK-031
title: Compose templates with C# source files
status: done
priority: high
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-018
tags:
  - csharp
  - composition
  - compiler
  - templates
createdAt: 2026-08-08T05:05:52.403Z
updatedAt: 2026-08-08T06:09:50.339Z
refinementState: ready
---

Compile trusted, ordinary C# source files alongside generated templates so templates can share
helper types through composition without inheriting a common page or importing executable Razor.

## Selected contract

- `@compileSource "path.cs"` adds one project or registered source to the current template
  compilation.
- `AddCSharpSource("path.cs")` adds a project source to every template compiled by the engine.
- `AddCSharpSource("logical.cs", sourceText)` registers an in-memory source and adds it globally;
  per-template directives can also resolve registered logical keys.
- Imported files are normal C# compilation units and are emitted into each consuming template's
  dynamic assembly. Helpers should normally be internal and must not escape as model or host API
  types.
- Top-level executable statements are rejected; imported code follows the same trusted-code policy
  as templates.

## Acceptance criteria

- [x] Razor recognizes multiple `@compileSource` directives without emitting them as template output.
- [x] Relative source keys resolve from the consuming project template; rooted/project-relative keys,
      global sources, and registered in-memory sources have deterministic normalization.
- [x] File, embedded-resource, custom-project, and string-template-with-project sources work; an
      actionable diagnostic is returned when no source provider can resolve a key.
- [x] Imported `.cs` files are separate Roslyn syntax trees with C# 14 parsing, logical source paths,
      portable symbols, and normal compiler diagnostics.
- [x] Duplicate normalized source keys are compiled once, while conflicting types or invalid code
      produce standard mapped C# diagnostics.
- [x] Main-template expression rewriting does not rewrite imported source trees.
- [x] Project change tokens for the template and every imported source are combined so changing a
      shared source invalidates every dependent compiler and page-factory cache entry.
- [x] Source traversal outside a configured project root and non-`.cs` imports are rejected.
- [x] Runtime and precompile paths use the same source bundle; no source file is rendered or emitted
      as a standalone template artifact.
- [x] README, changelog, security guidance, public API evidence, and focused end-to-end tests document
      composition, duplication/type-identity limits, and trusted-code implications.

## Scope boundaries

Do not import Razor `@functions`, merge generated template classes, compile a shared companion
assembly, support C# scripts/top-level statements, or claim cross-template type identity. Those are
different language/runtime designs and require separate tasks if later justified.

## Implementation notes

- Registered a file-scoped, repeatable `@compileSource` directive and captured its normalized source
  dependencies during Razor IR processing without adding generated template output.
- Added global project and in-memory source configuration to fluent and dependency-injection builders;
  custom projects can override `GetSourceItemAsync` for exact `.cs` lookup.
- Compiled imported files as separate Roslyn syntax trees while restricting expression rewriting to
  the generated template tree. Logical source keys flow through compiler diagnostics and portable
  symbols.
- Combined source and template change tokens before publishing compiler-cache entries. Source loading
  and Razor generation remain outside the cache lock, and page-factory invalidation follows the same
  dependency token.
- Covered global string sources, relative file sources, embedded resources, custom-project change
  tokens, duplicate paths, traversal and extension rejection, top-level statement rejection, mapped
  diagnostics, and the command-line precompile path.
