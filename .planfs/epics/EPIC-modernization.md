---
id: EPIC-modernization
title: Modernize and secure RazorLight
status: active
owner: justin
description: Evolve RazorLight into a supported, secure, compatible, and independently releasable .NET template engine.
targetDate: 2027-03-31
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-07T00:29:26Z
---

Modernize the inherited RazorLight repository without losing the compatibility information needed by
existing users, then resolve the runtime and language gaps that accumulated while upstream was
inactive.

## Overview

The modernization foundation was completed in TASK-001 through TASK-007 and merged in pull request
9. The next phases establish independent package publishing, align template behavior with current
Razor and C#, define supported deployment modes, and modernize the public library surface without
hiding compatibility changes.

## Child tasks

### Modernization foundation

- TASK-001 through TASK-007: completed and merged

### Release readiness

- TASK-008: Define package identity and release automation
- TASK-016: Add package and API compatibility validation
- TASK-020: Reconcile Dependabot with central package management

### Language and runtime compatibility

- TASK-010: Make LINQ and imports consistent across template sources
- TASK-011: Establish a supported current Razor compiler integration
- TASK-012: Define single-file, trimming, and Native AOT compatibility
- TASK-014: Make template caching and invalidation coherent
- TASK-019: Reduce or justify the ASP.NET Core runtime dependency
- TASK-021: Align dependency injection and ViewBag behavior
- TASK-022: Build a supported precompiled-only execution mode

### Library quality

- TASK-009: Adopt nullable reference types incrementally
- TASK-013: Add cancellation to asynchronous operations
- TASK-015: Define and enforce the template trust boundary
- TASK-017: Ratchet coverage and establish performance baselines
- TASK-018: Design the next-major public API cleanup

## Success criteria

- Repository expectations and project decisions are explicit.
- Supported frameworks and package compatibility are documented and tested.
- Known vulnerable dependencies and unsupported build requirements are removed.
- CI provides trustworthy cross-platform build and test results.
- The first independent package can be produced and published through a controlled process.
- Current C# and Razor syntax has an explicit, tested support matrix.
- String, file, embedded, and custom template sources have intentional import and model semantics.
- Deployment, caching, cancellation, and untrusted-template limitations are explicit and testable.
