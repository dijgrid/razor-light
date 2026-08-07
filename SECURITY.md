# Security policy

## Supported versions

This repository has not published an independently maintained release yet. Security fixes are
developed against the default branch. The inherited `2.3.1` release line receives best-effort
review until a new compatibility and release policy is published.

Older upstream releases are not actively supported here.

## Template trust boundary

Razor templates are executable .NET code and must be trusted when compiled or rendered in the host
process. HTML encoding protects rendered output; it does not sandbox C# execution. Metadata
reference controls reduce accidental API exposure but are not an in-process security boundary.

Run user-authored or otherwise untrusted templates only in a separately secured process or service
with its own identity, restricted files and network, no application secrets, and resource limits.
See the [template security and trust-boundary guide](docs/template-security.md) for the threat model,
reference controls, diagnostic policy, and residual risks.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use GitHub's
[private vulnerability reporting](https://github.com/dijgrid/razor-light/security/advisories/new)
to share the details with the maintainers.

Please include:

- the affected RazorLight and .NET versions
- a minimal reproduction or proof of concept
- the expected impact and known mitigations
- whether the report or reproduction can be shared publicly after a fix

The maintainers will acknowledge the report, investigate it, and coordinate disclosure through the
private advisory. Response and remediation times depend on severity and maintainer availability.
