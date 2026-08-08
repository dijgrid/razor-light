---
id: TASK-019
title: Reduce or justify the ASP.NET Core runtime dependency
status: review
priority: medium
epic: EPIC-modernization
milestone: MILESTONE-language-runtime-compatibility
dependsOn:
  - TASK-011
  - TASK-012
tags:
  - dependencies
  - deployment
  - packaging
  - compatibility
createdAt: 2026-08-07T00:29:26Z
updatedAt: 2026-08-08T00:11:33.703Z
refinementState: ready
---

Convert RazorLight into a generic C# text-template engine whose core has no built-in HTML behavior or
`Microsoft.AspNetCore.App` shared-framework requirement. HTML encoding and ASP.NET interoperability
belong in an optional integration package and must not be privileged by the core design.

## Acceptance criteria

- [x] An assembly and feature inventory identifies which `Microsoft.AspNetCore.App` components the
      library actually uses at compile time and runtime.
- [x] The core package has no `FrameworkReference` to `Microsoft.AspNetCore.App`, and a
      framework-dependent consumer runs with only `Microsoft.NETCore.App` installed.
- [x] ASP.NET-owned types are removed from the core public API and generated-template contract;
      replacement content, body, section, helper-result, and compiled-template metadata contracts
      are owned by RazorLight.
- [x] Core rendering writes expression values as generic text by default and exposes a pluggable,
      content-neutral output transformation policy for format-specific escaping.
- [x] Razor expression output, literal output, raw values, layouts, sections, includes, attribute
      code generation, buffering, and asynchronous helper results have focused regression coverage.
- [x] HTML encoding, `IHtmlContent` interoperability, and tag helpers are absent from the core and
      available only through an optional integration package if maintained.
- [x] `Microsoft.Extensions.*` APIs used by the core are supplied by supported standalone package
      references rather than implicitly by the ASP.NET Core shared framework.
- [x] Console, worker, desktop, framework-dependent, and self-contained consumers have publish and
      smoke-test coverage representative of the supported distribution model.
- [x] Package size and deployment footprint are measured before and after the decision.
- [x] No unsupported copy of framework assemblies is embedded merely to avoid a shared-framework
      requirement.
- [x] README and migration guidance distinguish Razor syntax from HTML behavior and show generic-text
      and optional HTML integration configuration.

## Baseline findings

`RazorLight.csproj` currently carries a `FrameworkReference` to `Microsoft.AspNetCore.App`, so a
framework-dependent console or desktop consumer also requires the ASP.NET Core shared runtime.
Upstream issue [`#360`](https://github.com/toddams/RazorLight/issues/360) records this unresolved
distribution concern.

The built `RazorLight.dll` directly references five relevant ASP.NET Core assemblies/namespaces:

- `Microsoft.AspNetCore.Html.Abstractions` supplies `IHtmlContent`, `IHtmlContentBuilder`,
  `IHtmlContentContainer`, and `HtmlString`.
- `Microsoft.AspNetCore.Razor.Runtime` supplies hosting metadata and tag-helper runtime types.
- `Microsoft.AspNetCore.Razor` supplies current shared-framework Razor runtime types.
- `Microsoft.AspNetCore.Razor.Language` and `Microsoft.AspNetCore.Mvc.Razor.Extensions` supply the
  separately packaged Razor 6 compiler integration retained by TASK-011.

The public API baseline contains 20 signatures involving `Microsoft.AspNetCore.Html`, four involving
ASP.NET Core tag-helper namespaces, and one involving `Microsoft.AspNetCore.Razor.Hosting`.
`Microsoft.Extensions.Caching`, dependency injection, options, primitives, and file-provider APIs are
also currently resolved from the shared framework, but current standalone packages exist for these
and they are not architectural blockers.

The installed .NET 10.0.8 ASP.NET Core shared runtime is approximately 28.46 MiB across 146 files.
Existing deployment probes produce approximately 29.62 MiB framework-dependent, 134.44 MiB
self-contained win-x64, and 128.89 MiB single-file win-x64 outputs. These are baselines, not claimed
savings: a controlled before/after publish comparison is required.

## De-HTML-ification findings

Razor syntax and most RazorLight features are not inherently HTML-specific. Model expressions,
control flow, imports, dependency injection, layouts, sections, includes, file/embedded/string
projects, caching, and runtime compilation can all remain in a generic-text engine. The HTML coupling
is concentrated in the execution contract inherited from MVC:

- `TemplatePageBase.Write` HTML-encodes ordinary expression values while `WriteLiteral` bypasses
  encoding. `RawString` already demonstrates a RazorLight-owned bypass contract.
- Layout bodies, section sentinel values, helper results, and view buffers use `IHtmlContent` mostly
  as a write-to-`TextWriter` abstraction; they do not require an HTTP response or MVC view context.
- Tag-helper factory, scope, attribute, content, and execution APIs are embedded in
  `TemplatePageBase`. No dedicated tag-helper tests were found, so current support is not sufficiently
  characterized to preserve by assumption.
- `CompiledTemplateDescriptor.Item` and `RazorCompiledItemLoader` expose ASP.NET hosting metadata,
  but generated templates already carry RazorLight's own `RazorLightTemplateAttribute`. That
  attribute can become the canonical compiled-template identity without an ASP.NET runtime type.
- `HtmlEncoder` itself comes from `System.Text.Encodings.Web` in the base .NET runtime. Keeping an HTML
  encoder as one supported policy does not require `Microsoft.AspNetCore.App`.

Razor's parser remains markup-aware even for generic text. In particular, generated code can call
attribute-writing methods for markup-shaped input. Those methods should remain in the core generated
contract unless compiler tests prove they can be replaced. This task removes HTML ownership from the
runtime abstraction; it does not attempt to turn Razor into a format-agnostic parser or invent a new
templating language.

## Recommended design decision

1. Make the primary package a standalone, text-capable core targeting `Microsoft.NETCore.App` only.
2. Introduce a RazorLight-owned content contract for already-final output to replace `IHtmlContent`
   in bodies, buffers, sections, and helper results. Reuse or evolve `IRawString` rather than creating
   parallel concepts without need.
3. Replace the negative `DisableEncoding` concept with a content-neutral output transformation
   policy. Core expression output is identity/plain text by default because no single escaping rule
   is correct for HTML, XML, JSON, source code, shell scripts, or other targets. Format-specific
   escaping is opt-in.
4. Make `RazorLightTemplateAttribute` and RazorLight-owned descriptors canonical for both runtime and
   precompiled templates; remove `RazorCompiledItem` from the core public surface.
5. Keep the core Razor generated-code methods needed for expressions, literals, layouts, sections,
   and markup attributes. Reject `@addTagHelper` with an actionable diagnostic when the optional
   integration is absent.
6. Remove HTML encoding, tag-helper APIs, and `IHtmlContent` interoperability from core. Offer them
   only in an optional integration package if characterization tests establish a supportable
   contract. The core package must not reference that package or shared framework.
7. Add explicit current `Microsoft.Extensions.*` package dependencies for the pieces retained by the
   core.

This is an intentional next-major API break and should be coordinated with TASK-018. TASK-022 should
consume the same RazorLight-owned compiled-template metadata so precompiled-only deployments receive
the standalone-runtime benefit without maintaining a second template contract.

The generic core's raw default is also an intentional behavioral break. Migration documentation must
warn HTML-producing consumers to install and explicitly select the HTML integration before upgrading;
the trust-boundary guidance from TASK-015 remains applicable because output escaping cannot make
untrusted templates safe to execute.

## Implementation slices

1. Add characterization tests for encoding, raw output, markup attributes, buffers, layouts,
   sections, includes, helper results, and any tag-helper behavior that is claimed as supported.
2. Introduce the core content and encoding abstractions, migrate internal buffering/rendering, and
   add a first-class plain-text builder option.
3. Replace ASP.NET compiled-item discovery with RazorLight metadata and update the precompile path.
4. Isolate or remove tag-helper integration, then delete the core framework reference and add
   explicit supported package dependencies.
5. Add deployment probes that fail if `Microsoft.AspNetCore.App` reappears, measure the resulting
   package/publish footprint, and document migration.

## Implementation notes

- Removed the `Microsoft.AspNetCore.App` framework reference and replaced implicitly supplied
  `Microsoft.Extensions.*` assemblies with explicit .NET 10 package dependencies. The retained
  `Microsoft.AspNetCore.Razor.Language` and `Microsoft.AspNetCore.Mvc.Razor.Extensions` dependencies
  are compiler packages; they do not add an ASP.NET Core shared-runtime requirement.
- Added RazorLight-owned `ITemplateContent`, `TemplateContent`, and `IOutputEncoder` contracts.
  `PlainTextEncoder` is the core default, `WriteLiteral` and final content bypass transformation,
  and custom format encoders can be selected with `UseOutputEncoder`.
- Replaced `IHtmlContent` in bodies, sections, helper results, and buffers, and replaced ASP.NET
  compiled-item discovery with `RazorLightTemplateAttribute`. Removed the core tag-helper runtime
  surface; tag-helper directives now fail generation with an actionable diagnostic.
- Added the optional `Dijgrid.RazorLight.Html` package. Calling `UseHtmlEncoding()` restores explicit
  HTML expression encoding using the base-runtime `System.Text.Encodings.Web.HtmlEncoder`; the
  package deliberately does not restore MVC `IHtmlContent` or tag helpers.
- Added console, generic-host worker, and Windows desktop deployment probes. Their runtime configs
  are checked for accidental `Microsoft.AspNetCore.App` references; console self-contained and
  single-file modes remain covered as well.

## Results

The win-x64 console publish footprint changed from approximately 29.62 MiB to 24.12 MiB for the
framework-dependent output, 134.44 MiB to 100.48 MiB for self-contained output, and 128.89 MiB to
94.35 MiB for single-file output. File counts fell from 347 to 217 and from 677 to 404 in the first
two modes; single-file remained three files.

The core package changed from approximately 131.93 KiB to 121.88 KiB. The optional HTML integration
package is approximately 23.41 KiB. Package validation confirms that neither package embeds copied
framework assemblies.

Validation completed with a warning-free Release solution build, 252 core tests, 120 precompile
tests, deterministic DLL/PDB verification, all deployment probes, and package-layout validation for
`Dijgrid.RazorLight`, `Dijgrid.RazorLight.Html`, and `Dijgrid.RazorLight.Precompile`.
