# Cancellation

RazorLight 3.x supports cooperative cancellation across template lookup, compilation waits,
rendering, layouts, includes, source composition, and the precompile tool. Existing overloads remain
binary compatible and delegate with `CancellationToken.None`.

## Engine operations

Every asynchronous `IRazorLightEngine` operation has a token overload. Convenience overloads accept
a token without requiring a `ViewBag`; overloads that accept both place the token last.

```csharp
using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

string output = await engine.CompileRenderStringAsync(
    "invoice",
    templateText,
    model,
    cancellationSource.Token);
```

A token cancelled before invocation prevents lookup, compilation, rendering, and cache population.
During rendering, RazorLight checks the token at page, layout, include, section, flush, and final
output boundaries. Synchronous template code and synchronous `TextWriter` methods cannot be
preempted; template code performing asynchronous work should pass the inherited
`CancellationToken` property to that work.

Rendering owns the page context, pooled output buffers, and any dependency-injection scope used by
the page. RazorLight therefore does not abandon an in-flight page, include, section, or output write
merely to return cancellation promptly. Token-aware operations receive the render token and can stop
cooperatively. If template code or a writer ignores that token, RazorLight awaits the owned operation
before releasing its resources and then observes cancellation at the next safe boundary.

## Shared compilation

Compilation tasks are cached and may have multiple waiters. Cancelling one caller cancels that
caller's wait, not an already-published shared compilation required by another caller. Before a
compilation is published, cancellation is passed through project lookup, import lookup, C# source
lookup, source reading, and the compile-lock wait. A cancelled or failed attempt is not retained as
a poisoned cache entry, so a later call can retry the same key.

Roslyn emission is synchronous once it starts and cannot be interrupted safely. Cancellation is
checked immediately before and after source generation and at the surrounding asynchronous
boundaries.

This shared-compilation behavior is intentionally different from rendering: a compilation waiter
does not exclusively own the shared task, while a renderer exclusively owns its page, buffers, and
scope until rendering completes.

## Project implementations

`RazorLightProject` retains its original abstract methods and adds virtual token-aware overloads.
This preserves existing custom project implementations. The default token overload cancels the wait
around the legacy method but cannot stop I/O owned by that implementation.

New or updated projects should override token-aware methods and pass the token to their underlying
storage API:

```csharp
public override async Task<RazorLightProjectItem> GetItemAsync(
    string templateKey,
    CancellationToken cancellationToken)
{
    string content = await store.ReadAsync(templateKey, cancellationToken);
    return new TextSourceRazorProjectItem(templateKey, content);
}
```

The same rule applies to `GetImportsAsync`, `GetSourceItemAsync`, and `GetKnownKeysAsync`.

## Templates and composition

`PageContext.CancellationToken` is shared by the top-level page, layouts, and includes.
`TemplatePageBase.CancellationToken` exposes it naturally to generated template code. Explicit token
overloads are also available for `IncludeAsync`, `RenderSectionAsync`, and `FlushAsync`. An explicit
include token is passed to both child-template lookup and child rendering. Parameterless include,
section, and flush helpers use the active page token.

## Command-line tool

The precompile and render commands pass console cancellation through engine operations. Pressing
Ctrl+C requests cancellation and causes the command to exit with status code 130. Work already in a
non-cancellable synchronous compiler or file-system operation may finish before the process exits.
