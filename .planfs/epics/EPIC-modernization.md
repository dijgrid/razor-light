---
id: EPIC-modernization
title: Modernize and secure RazorLight
status: active
owner: justin
description: Establish a supported, secure, testable, and independently releasable RazorLight baseline.
targetDate: 2026-10-31
createdAt: 2026-08-06T00:00:00Z
updatedAt: 2026-08-06T00:00:00Z
---

Modernize the inherited RazorLight repository without losing the compatibility information needed by
existing users.

## Overview

The current project targets several end-of-life .NET versions, carries old package and test
dependencies, and inherited release automation that is unsuitable for an independent maintainer.
The work is sequenced to capture a baseline first, then modernize frameworks and dependencies, and
only then establish independent package publishing.

## Child tasks

- TASK-001: Establish repository governance and PlanFS
- TASK-002: Capture the compatibility and package baseline
- TASK-003: Move to supported target frameworks
- TASK-004: Centralize and secure package dependencies
- TASK-005: Modernize the test suite and quality gates
- TASK-006: Resolve implementation warnings and obsolete APIs
- TASK-007: Refresh documentation and samples
- TASK-008: Define package identity and release automation

## Success criteria

- Repository expectations and project decisions are explicit.
- Supported frameworks and package compatibility are documented and tested.
- Known vulnerable dependencies and unsupported build requirements are removed.
- CI provides trustworthy cross-platform build and test results.
- The first independent package can be produced and published through a controlled process.
