# 0002 — Form factor: Core library + CLI + Web

- **Status:** Accepted (2026-06-04)

## Context
ContextTax needs an embeddable, testable measurement engine, a way to run it and see
a report, and (later) a shareable dashboard / leaderboard.

## Decision
Three projects: **`ContextTax.Core`** (engine library), **`ContextTax.Cli`** (report
card), **`ContextTax.Web`** (dashboard, later). `Cli` and `Web` depend on `Core`;
`Core` depends on nothing else in the solution.

## Consequences
- Clean separation, reusable engine, strongest showcase.
- More projects to scaffold up front than a single CLI would need.
