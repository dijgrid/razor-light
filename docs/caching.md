# Caching and invalidation

RazorLight uses two caches during runtime compilation. They have different contents but one
observable invalidation contract:

| Layer | Contents | Owner |
| --- | --- | --- |
| Compilation cache | In-flight and completed `CompiledTemplateDescriptor` tasks | Internal `RazorTemplateCompiler` |
| Page-factory cache | Factories that create executable `ITemplatePage` instances | The configured `ICachingProvider`, hidden behind the engine facade |

The built-in compiler and engine coordinate these layers. Calling `engine.InvalidateTemplate(key)`
removes all known compiled variants and page factories for that logical key. Applications can call
`engine.IsTemplateCached(key)` without receiving the provider's page-factory records. Provider-level
`CacheTemplate` is a replacement: it invalidates old compiler and provider entries before storing
the supplied factory. A compilation that was already in flight when a key was removed or replaced
cannot repopulate the page-factory cache with its stale result.

Compilation failures retain their exception type and diagnostic IDs. Detailed template-derived
messages and mapped paths are available only when debug mode is enabled, as described in the
[template security guide](template-security.md). A failed task is removed from the compilation cache
so correcting the source and retrying the same key does not require a process restart.

## Cache identity and project changes

String-template compilation identity includes the logical key, source content, explicit model type,
and configured imports. Changing any of those inputs replaces the previous entry for the logical
key, including the alias used by layouts and includes.

Project-backed templates use the `IChangeToken` returned by `RazorLightProjectItem.ExpirationToken`.
The token expires both the descriptor and page-factory entries for that template. Layouts and
includes are cached under their own keys, so their own project-item tokens invalidate them even when
the page that references them remains cached. A custom project should therefore return a token for
every item whose source can change.

## Keys and concurrency

Project normalization is applied before cache lookup. File-style keys use forward slashes as their
canonical separator and may be supplied with either slash form. Comparisons after normalization are
ordinal and case-sensitive on every operating system; applications should not rely on the host file
system's casing behavior.

The coordinator serializes replacement and removal bookkeeping, but retrieval and the configured
provider's own work can occur concurrently. Custom `ICachingProvider` implementations must make
`TryGetTemplate`, `CacheTemplate`, `Contains`, and `Remove` safe for concurrent callers.

The built-in `PrecompiledCachingProvider` follows the same separator and case rules. `CacheTemplate`
can add or replace a runtime page factory, and `Remove` deletes both that runtime entry and the
matching precompiled entry from that provider instance. Removing an unknown key is idempotent.

This coordination is process-local. Distributed applications that change shared template sources
must still deliver a project change token or call `InvalidateTemplate` in each process.
