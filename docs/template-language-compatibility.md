# Template language compatibility baseline

This note records RazorLight's current behavior on the maintained .NET 10 baseline. It is a
characterization, not a promise that the known limitations are desirable. The executable evidence
lives in `TemplateLanguageCompatibilityTest`.

## Source and import matrix

| Source | Built-in imports | `AddDefaultNamespaces` | Explicit `@using` | No import |
| --- | --- | --- | --- | --- |
| String content | Not applied | Not applied | Applied | LINQ extension methods are unavailable |
| File project | Applied | Applied | Applied | Built-in imports still apply |
| Embedded-resource project | Applied | Applied | Applied | Built-in imports still apply |
| Custom/in-memory project item | Applied | Applied | Applied | Built-in imports still apply |

String content is represented internally by `TextSourceRazorProjectItem`. The current source
generator returns no imports for that item type, so it skips both the built-in imports (including
`System.Linq`) and namespaces configured with `AddDefaultNamespaces`. An explicit `@using` in the
string itself works. Project-backed items receive built-in imports and configured namespaces.

## Models and LINQ

An explicit `@model` produces a strongly typed template. `Any`, `Where`, `Select`, and
`FirstOrDefault` work when `System.Linq` is in the effective imports. A generic render call does not
infer its generic argument as the generated template model when `@model` is absent; the generated
base remains `TemplatePage<dynamic>`.

Simple member access works for a generic model, anonymous object, `ExpandoObject`, and a dynamic
receiver when the template has no `@model`. A lambda passed to an extension-method-shaped call on a
dynamic receiver does not compile, even with `@using System.Linq`; C# reports that a lambda cannot be
an argument to a dynamically dispatched operation. This dynamic-binding limitation is distinct
from a missing import.

The tests assert the relevant generated-code fragments and diagnostic messages rather than complete
generated files, paths, or compiler versions.

## String-template cache identity

The template key is currently the compilation cache identity. Reusing a key with different content
or imports continues to execute the first compiled template. Reusing it with a different explicit
model type also reuses the first compiled template and fails while assigning the incompatible model.
Callers should treat string-template keys as immutable and unique for the effective content, model
type, and imports until TASK-010 and TASK-014 establish an intentional invalidation contract.
