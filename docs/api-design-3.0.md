# RazorLight 3.x API design

RazorLight 3.x is a deliberate compatibility reset: it targets .NET 10, uses the independently owned
`Dijgrid.*` package IDs, treats RazorLight as a generic C# text-template engine, and makes HTML output
encoding optional. The first beta should expose a surface that can be supported for the full 3.x
line rather than preserving inherited implementation details by accident.

## Post-TASK-019 inventory

The Roslyn public API record currently contains 657 entries.

| Area | Evidence | 3.x disposition |
| --- | --- | --- |
| Obsolete/error-only API | `EngineFactory`, `IEngineFactory`, three engine overloads that cannot compile at call sites and throw if invoked indirectly | Remove in TASK-027 and document builder/supported-overload replacements. |
| Retired compatibility | `LegacyFixAssemblyPathFormatter`, `UseNetFrameworkLegacyFix`, and the redundant file-system engine factory | Remove in TASK-027; the independent line supports .NET 10, not .NET Framework. |
| Engine object graph | `IRazorLightEngine.Options` is mutable; `Handler` exposes cache, compiler, and page factory | Replace with a stable facade and narrow cache administration in TASK-028. |
| Mutable configuration | Six replaceable collection properties, callbacks, cache, assembly, encoder, and debug mode remain mutable after build | Snapshot at build/DI resolution; configure through builders/registration. |
| Cache extension | `ICachingProvider` mixes application invalidation with storage of page factories and change tokens | Split cache administration from provider storage while preserving custom providers. |
| DI and page activation | Singleton engine mutation installs property injection; resolving `IEngineHandler` intentionally throws | Establish per-render scopes and page initialization in TASK-021. |
| Project extension | `RazorLightProject` and `RazorLightProjectItem` are used for file, embedded, database, and custom sources | Retain as supported extension points; add cancellation separately in TASK-013. |
| Compiler/generation | Public compiler services, source generator, Razor passes, metadata managers, and factories expose the Razor 6 adapter | Internalize unless an end-to-end extension test proves a supported use in TASK-029. |
| Buffering internals | 108 API entries mention `RazorLight.Internal`, primarily buffer pages, writers, pools, and fast setters | Internalize in TASK-029; generated templates do not consume these types directly. |
| Generated page ABI | Template page bases, write methods, context, helper/content contracts, injection and template metadata | Keep the minimum required public ABI and validate it separately from application APIs. |
| Sync-over-async | Synchronous section rendering and helper-result writes block on tasks; the CLI also blocks at its boundary | Add no new sync wrappers. Retain only generated-ABI requirements until code generation can remove them safely. |

Repository searches find normal consumers using the builder and engine methods, while tests account
for most direct compiler/buffer access. GitHub's indexed code search on 2026-08-08 returned 23 matches
for `.Handler.Cache`, 40 for `ICachingProvider`, 147 for `RazorLightProject`, and 102 for
`RazorLightProjectItem`. Compiler-interface results were mostly upstream forks or vendored copies.
These counts are directional evidence, not a telemetry claim or an exhaustive dependent-package
survey.

## Supported layers

The application layer should contain:

- `IRazorLightEngine` compile, render, and cache-administration operations;
- `RazorLightEngineBuilder` and the DI registration builder;
- `IOutputEncoder`, `ITemplateContent`, and the optional HTML integration;
- stable template, compilation, and configuration exceptions.

Supported extension points should contain custom project lookup, output encoding, cache storage, and
page initialization/service resolution. Each must have an end-to-end custom implementation test.

The generated-template ABI must contain only types referenced from emitted C#: page bases and page
contracts, page context, render delegates, final-content/helper types, injection metadata, and
template identity. Public visibility in this tier does not make compiler passes or buffers normal
application extension points.

## Source migration examples

TASK-027 removed the following inherited entry points before the first beta:

| Removed entry point | Supported replacement |
| --- | --- |
| `EngineFactory.ForFileSystem(...)`, `IEngineFactory.ForFileSystem(...)` | `new RazorLightEngineBuilder().UseFileSystemProject(root)` |
| `EngineFactory.ForEmbeddedResources(...)`, `IEngineFactory.ForEmbeddedResources(...)` | `new RazorLightEngineBuilder().UseEmbeddedResourcesProject(rootType)` |
| `EngineFactory.Create(options)`, `IEngineFactory.Create(options)` | `new RazorLightEngineBuilder().UseNoProject().UseOptions(options).Build()` |
| `EngineFactory.Create(project, options)`, `IEngineFactory.Create(project, options)` | `new RazorLightEngineBuilder().UseProject(project).UseOptions(options).Build()` |
| `IRazorLightEngineFactory`, `RazorLightEngineWithFileSystemProjectFactory` | `RazorLightEngineBuilder`, or `services.AddRazorLight()` for DI |
| `CompileRenderAsync(key, content, model, modelType, viewBag)` | `CompileRenderStringAsync(key, content, model, modelType, viewBag)` |
| Non-generic `RenderTemplateAsync(..., model, modelType, ...)` overloads | Generic `RenderTemplateAsync(page, typedModel, ...)` overloads |
| `UseNetFrameworkLegacyFix()` and `LegacyFixAssemblyPathFormatter` | Remove the call; the maintained .NET 10 path needs no workaround |
| `TemplateCompilationException(message, IEnumerable<string>)` | Construct `TemplateCompilationDiagnostic` values and use the structured constructor |

### Engine construction

Replace the inherited factory:

```csharp
// 2.x
RazorLightEngine? engine = new EngineFactory().ForFileSystem(root);

// 3.x
IRazorLightEngine engine = new RazorLightEngineBuilder()
    .UseFileSystemProject(root)
    .UseMemoryCachingProvider()
    .Build();
```

The exact obsolete factory method varied across inherited versions; the 3.x replacement is always
an explicit builder or DI registration.

Replace the redundant file-system factory the same way:

```csharp
// removed
IRazorLightEngine engine = new RazorLightEngineWithFileSystemProjectFactory().Create(
    operatingAssembly,
    root);

// supported
IRazorLightEngine engine = new RazorLightEngineBuilder()
    .SetOperatingAssembly(operatingAssembly)
    .UseFileSystemProject(root)
    .UseMemoryCachingProvider()
    .Build();
```

### String templates and runtime model types

Replace error-only overloads with the supported string method:

```csharp
string output = await engine.CompileRenderStringAsync(
    "invoice",
    templateText,
    model,
    model.GetType());
```

Use the generic overload when the model type is known at compile time.

The two error-only `RenderTemplateAsync` overloads that accepted a separate `Type` were also removed.
Use the supported generic overloads for string or writer output:

```csharp
string output = await engine.RenderTemplateAsync(page, typedModel, viewBag);
await engine.RenderTemplateAsync(page, typedModel, writer, viewBag);
```

### Retired .NET Framework workaround

Remove `UseNetFrameworkLegacyFix()` from dependency-injection registration. The 3.x line targets
.NET 10, and normal metadata discovery already uses supported `Assembly.Location` behavior.

### Compilation diagnostics

Code that constructed `TemplateCompilationException` from strings must preserve structured compiler
information explicitly:

```csharp
var diagnostic = new TemplateCompilationDiagnostic(
    errorMessage,
    formattedMessage,
    lineSpan);
var exception = new TemplateCompilationException(message, new[] { diagnostic });
```

### Cache invalidation

TASK-028 will replace the handler traversal with an engine-level operation:

```csharp
// inherited surface
engine.Handler.Cache?.Remove(templateKey);

// 3.x facade
engine.InvalidateTemplate(templateKey);
```

Cache provider configuration remains an extension point, but normal applications no longer handle
compiled page factories merely to invalidate a template.

### Configuration and page initialization

Move post-build option mutation into builder or DI registration:

```csharp
// inherited surface
engine.Options.Namespaces.Add("Application.TemplateContracts");
engine.Options.PreRenderCallbacks.Add(InitializePage);

// 3.x construction-time configuration
IRazorLightEngine engine = new RazorLightEngineBuilder()
    .AddDefaultNamespaces("Application.TemplateContracts")
    .AddPageInitializer(InitializePage)
    .Build();
```

TASK-021 owns the final initializer name and DI scope implementation. The design requirement is that
configuration is frozen before the engine is used.

## Binary and package migration

`RazorLight` 2.3.1 binaries are not compatible with the independent line. Consumers must change the
package reference to `Dijgrid.RazorLight`, target .NET 10, rebuild, and address the source migrations
above. HTML output consumers must also reference `Dijgrid.RazorLight.Html` and call
`UseHtmlEncoding()`.

Assemblies containing precompiled 2.x templates must be regenerated with the matching 3.x toolchain.
No cross-version precompiled-template guarantee is made before TASK-022 establishes executable ABI
validation.

## Beta sequence

1. TASK-027 removes inherited obsolete and unsupported APIs.
2. TASK-028 establishes the stable engine/cache facade.
3. TASK-021 establishes immutable configuration, per-render DI scopes, page initialization, and
   predictable ViewBag behavior.
4. TASK-029 internalizes implementation details and locks the generated-template ABI.
5. TASK-030 publishes `3.0.0-beta.1` from the reviewed artifacts.

TASK-013 cancellation, TASK-017 performance/coverage ratchets, and TASK-022 precompiled-only
execution can continue during the beta series without delaying the first useful prerelease unless
their implementation requires another public API break.
