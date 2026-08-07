---
id: DECISION-004
title: Treat in-process Razor templates as trusted code
status: accepted
date: 2026-08-07
author: justin
---

Treat every Razor template compiled or rendered in the application process as trusted executable
.NET code. Require a separately secured process or service for templates supplied by an untrusted
author.

## Context

RazorLight compiles templates into assemblies that run with the host process's permissions.
Metadata references affect ordinary compile-time name resolution, HTML encoding affects rendered
output, and dependency-injection configuration affects convenient service resolution. None of those
mechanisms prevents reflection, assembly loading, filesystem access, networking, or other actions
available to executable .NET code.

## Decision

- Do not advertise or implement an in-process untrusted-template mode.
- Use minimal metadata-reference discovery by default to reduce accidental host dependency exposure.
- Preserve exact include and exclude controls plus an explicit broad-discovery compatibility mode.
- Redact template-derived compiler messages, mapped paths, missing keys, and key inventories by
  default; expose full details only through the existing debug-mode opt-in.
- Treat generated assemblies, symbols, caches, and diagnostic artifacts as potentially sensitive.
- Document process isolation as an application architecture requirement rather than a RazorLight
  configuration switch.

## Consequences

Positive:

- Trusted-template hosts receive more deterministic dependencies and safer production diagnostics.
- Consumers can intentionally expose contract assemblies without inheriting every host package.
- Security documentation does not imply a sandbox that the runtime cannot enforce.

Negative:

- Templates that depended on unrelated NuGet packages through ambient discovery must include those
  assemblies explicitly or select broad compatibility discovery.
- Production compiler messages contain diagnostic IDs and locations but require debug mode for full
  Roslyn or Razor text.
- Safely accepting arbitrary template authors remains an external isolation and operations problem.
