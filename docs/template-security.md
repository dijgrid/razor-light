# Template security and trust boundary

Razor templates are executable .NET code. RazorLight compiles a template into an assembly and runs
that assembly inside the host process. Only execute templates supplied by the application, its
operators, or another party that is trusted to run code with the application's identity.

HTML encoding protects an output context. It can reduce HTML-injection risks in rendered output, but
it does not restrict the C# statements, APIs, reflection, file access, network access, or services a
template can use. Metadata-reference filtering likewise reduces accidental coupling; it is not a
security sandbox.

Sources added with `AddCSharpSource` or `@compileSource` are executable code under the same trust
boundary. Project-root checks constrain source lookup, not what the compiled helper can do once it
runs. Review and authorize changes to shared `.cs` sources exactly as you would changes to the host
application or its Razor templates.

## Supported trust models

| Template source | Supported architecture |
| --- | --- |
| Application-owned or operator-reviewed | Runtime compilation in the application process |
| Build-produced precompiled template | Load only artifacts produced and reviewed with the application |
| User-authored or otherwise untrusted | Compile and render in a separately secured process or service |

An isolated renderer for untrusted templates must have its own operating-system identity, minimal
filesystem and network access, no application secrets, bounded CPU and memory, and a narrow
serialized input/output contract. RazorLight does not create or claim such an isolation boundary.

## Metadata references

Minimal discovery is the default. It references application project assemblies, the operating
assembly, RazorLight's runtime dependency closure, and assemblies explicitly selected by the host.
Unrelated package dependencies in the host dependency context are not automatically available to
template compilation.

Use `IncludeAssemblies` to select an assembly by exact name from the dependency context, or
`AddMetadataReferences` to supply a specific Roslyn reference. `ExcludeAssemblies` denies an
automatically discovered exact assembly name; an explicit metadata reference remains an intentional
override. For compatibility, `UseAllDependencyContextMetadataReferences` restores broad discovery
of every compile-time dependency:

```csharp
var engine = new RazorLightEngineBuilder()
    .IncludeAssemblies("Application.TemplateContracts")
    .ExcludeAssemblies("Application.Internal.Data")
    .Build();
```

These controls improve dependency isolation and reproducibility. They do not stop a template from
using reflection, loading assemblies available to the process, or calling powerful APIs through an
allowed type.

## Dependency injection

`@inject` resolves from the host service provider. A template can request any resolvable service for
which it can compile a type reference, and reflection can bypass compile-time reference
conveniences. Do not put secrets or privileged capabilities into a renderer's service provider on
the assumption that templates cannot reach them. Use a deliberately limited provider when that is
useful for application design, not as a sandbox for malicious code.

## Files, generated assemblies, and diagnostics

File-system projects read within their configured project root, but template code itself runs with
the host process's filesystem permissions. Embedded and custom projects similarly control lookup,
not what executed C# can access.

File-system containment is lexical: RazorLight canonicalizes the root and candidate path and rejects
keys whose full path escapes that root. A host that places a symbolic link or Windows reparse point
inside the root can still make content outside the root reachable through that link. Because project
content is trusted code, hosts that require a strict physical boundary must prevent such links in
the configured tree or validate them before making the project available.

Generated assemblies and symbols can contain template logic, string constants, type names, and
template keys. Treat precompile outputs, runtime compilation callbacks, caches, crash dumps, and
diagnostic artifacts as potentially sensitive.

Detailed template diagnostics are disabled by default. Compiler errors retain diagnostic IDs and
line positions while suppressing template-derived messages and mapped paths. Razor generation
errors similarly retain IDs without exposing their detailed messages or paths. Calling
`EnableDebugMode()` restores detailed messages, mapped paths, missing-template keys, and known-key
lists; enable it only in trusted development or controlled diagnostic environments.

## Cache integrity

String-template cache identity includes the logical key, source content, model type, and imports.
Changing any of those inputs invalidates the prior compiled variant. Project items use normalized
keys and change tokens, and failed compilation entries are removed before retry. These guarantees
prevent accidental cross-template reuse inside one engine instance; they do not authenticate a
shared template source. Applications remain responsible for authorizing writes to template stores
and for separating tenants when keys or stores are not mutually trusted.

## Residual risk

The accepted project decision is that arbitrary Razor cannot be safely sandboxed in-process.
Reference filtering, limited dependency injection, encoding, and cache validation are defense in
depth for trusted-template applications. Untrusted-template execution requires an external
isolation architecture with controls owned by the host application.
