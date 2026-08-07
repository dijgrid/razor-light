# Template language compatibility policy

This note records RazorLight's supported model, import, LINQ, and string-template identity behavior
on the maintained .NET 10 line. The executable policy lives in
`TemplateLanguageCompatibilityTest`.

Generated template code is intentionally parsed as C# 14. `CurrentCompilerCompatibilityTest`
covers collection expressions, raw strings, property patterns, nullable directives, async code,
representative Razor directives, generated-code fragments, and a stable Razor diagnostic. The
[compiler integration decision](razor-compiler-integration.md) explains the retained Razor parser
boundary and its lifecycle.

## Source and import matrix

| Source | Built-in imports | `AddDefaultNamespaces` | Explicit `@using` | No import |
| --- | --- | --- | --- | --- |
| String content | Applied | Applied | Applied | Built-in imports still apply |
| File project | Applied | Applied | Applied | Built-in imports still apply |
| Embedded-resource project | Applied | Applied | Applied | Built-in imports still apply |
| Custom/in-memory project item | Applied | Applied | Applied | Built-in imports still apply |

All source types receive the built-in imports (`System`, `System.Collections.Generic`,
`System.Linq`, and `System.Threading.Tasks`) and namespaces configured with
`AddDefaultNamespaces`. Explicit `@using` directives continue to work. String content has no
project location, so it does not inherit project import files; place required imports in the
content or configure them on the engine.

## Models and LINQ

An explicit `@model` produces a strongly typed template. `Any`, `Where`, `Select`, and
`FirstOrDefault` work with the built-in `System.Linq` import. A generic render call does not infer
its generic argument as the generated template model when `@model` is absent; the generated base
remains `TemplatePage<dynamic>` to preserve anonymous-object and `ExpandoObject` behavior.

Callers that cannot place `@model` in the template can select a visible, closed model type:

```csharp
string result = await engine.CompileRenderStringAsync(
    "orders",
    template,
    model,
    typeof(OrderModel));

string projectResult = await engine.CompileRenderAsync(
    "orders.cshtml",
    model,
    typeof(OrderModel));
```

The selected type supplies the model directive only when the template does not declare one. An
explicit template `@model` remains authoritative. Anonymous, inaccessible, and open generic types
cannot be referenced from the generated assembly and are rejected with an argument error; use the
generic dynamic path for anonymous and `ExpandoObject` models.

Simple member access works for a generic model, anonymous object, `ExpandoObject`, and a dynamic
receiver when the template has no `@model`. A lambda passed to an extension-method-shaped call on a
dynamic receiver does not compile, even with `@using System.Linq`; C# reports that a lambda cannot be
an argument to a dynamically dispatched operation. This dynamic-binding limitation is distinct
from a missing import. RazorLight appends guidance to that diagnostic directing callers to
`@model` or an explicit model-type overload.

The tests assert the relevant generated-code fragments and diagnostic messages rather than complete
generated files, paths, or compiler versions.

## String-template cache identity

String compilation identity includes the logical template key, content, selected model type, and
configured namespaces. Reusing a key with changed context removes the previous compiler and
configured-provider entries, compiles the replacement, and makes the replacement available under
the logical key for layouts and includes. The tests exercise both the internal compiler cache and
the optional memory caching provider.

Keys should still identify one logical template. Cross-process invalidation, project import-file
changes, bounded retention of historical variants, and the complete custom-provider contract remain
in TASK-014.

## Migration from the inherited behavior

- String templates now see the built-in and configured namespaces. If a template has a newly
  ambiguous type name, qualify that type or add an alias in the template.
- Reusing a string key with changed content no longer renders the first compiled version. Code that
  depended on immutable-first behavior should use a distinct key for each logical template instead.
- `CompileRenderAsync(key, model, modelType)` now performs typed project compilation instead of
  being an obsolete error-only entry point. Use the corresponding `CompileRenderStringAsync`
  overload for direct string content.
- Generic rendering without `@model` remains dynamic; no automatic `TModel` inference was added.
