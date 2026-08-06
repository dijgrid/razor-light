---
id: DECISION-002
title: Use PlanFS for repository task management
status: accepted
date: 2026-08-06
author: justin
---

Track planned engineering work as versioned PlanFS records in the repository.

## Context

The modernization effort spans framework support, dependencies, security, testing, documentation,
implementation cleanup, and package releases. A repository-native backlog keeps that work close to
the code and makes task state available to humans and coding agents without relying on an external
service.

## Decision

Use `.planfs` Markdown records for tasks, epics, milestones, and decisions. Each implementation pull
request should update the corresponding task status and acceptance criteria.

GitHub issues remain the public intake channel for bugs, questions, and feature requests. Maintainers
translate accepted work into PlanFS tasks when it joins the roadmap.

## Consequences

Positive:

- Plans and decisions are reviewable and versioned with the code.
- Work has explicit dependencies and acceptance criteria.
- Agents can discover project priorities without external credentials.

Negative:

- Task state changes require repository commits.
- Maintainers must prevent GitHub issues and PlanFS tasks from becoming competing backlogs.
