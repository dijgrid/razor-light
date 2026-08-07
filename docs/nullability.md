# Nullable reference types

Starting with the independently maintained version 3 line, `Dijgrid.RazorLight` publishes nullable
reference annotations. This does not change CLR signatures or intentionally change rendering
behavior. It gives nullable-enabled consumers compiler diagnostics for contracts that were
previously implicit.

## Rendering models and view data

Template keys, template content, template pages, model types, and writers are required. The optional
`viewBag` parameter is annotated as nullable. A model can be null when its generic type permits null:

```csharp
Order? order = FindOrder();
string result = await engine.CompileRenderAsync<Order?>("orders/detail", order, viewBag: null);
```

If a non-nullable model is required by the template, validate it before rendering instead of using
the null-forgiving operator:

```csharp
Order order = FindOrder() ?? throw new InvalidOperationException("The order was not found.");
string result = await engine.CompileRenderAsync("orders/detail", order);
```

The obsolete, error-only `EngineFactory` API is now accurately annotated as returning a nullable
engine. New code should continue to use `RazorLightEngineBuilder`.

## Template page lifecycle

Several `ITemplatePage` properties are populated by the renderer and may be null on a newly created
page: `PageContext`, `BodyContent`, `Layout`, `PreviousSectionWriters`, and `IncludeFunc`. Custom page
code that accesses these properties outside normal rendering must check them first:

```csharp
PageContext context = page.PageContext
    ?? throw new InvalidOperationException("The page has not entered the rendering pipeline.");
```

The `RenderSection` and `RenderSectionAsync` overloads with `required: false` return null when the
section is absent. The overloads that always require a section remain non-nullable.

## Configuration, caching, and compilation

`RazorLightOptions.CachingProvider` and `OperatingAssembly` are optional until supplied or resolved
by the builder. `ICachingProvider.CacheTemplate` accepts a null expiration token for entries without
change-based invalidation. Descriptor metadata such as `CompiledTemplateDescriptor.Item`,
`TemplateAttribute`, `ExpirationToken`, and computed `Type` can also be absent while a descriptor is
being constructed or when metadata is unavailable.

When implementing RazorLight interfaces, copy their nullable annotations. In particular, custom
caching providers should accept `IChangeToken?`, and custom template pages should accept `object?`
in `SetModel` because layouts and nullable model types can propagate a null model.

Do not silence new diagnostics globally. Prefer a null check for lifecycle state, a nullable local or
generic argument for an intentionally absent value, or an exception at the boundary where a value is
required.
