# Architecture

## Solution layout
- `src/ContextTax.Core` — measurement engine (library). No UI, no I/O frameworks;
  pure and testable. Home of token-counting, lifecycle measurement, and strategy
  comparison logic (built in later sub-projects).
- `src/ContextTax.Cli` — console entry point. Thin: parse args, call Core, render the
  report card. Depends on Core.
- `src/ContextTax.Web` — ASP.NET Core report / dashboard host. Thin: serve results
  from Core. Depends on Core. (Web tech — Blazor vs static vs API+SPA — decided in the
  web sub-project.)
- `tests/ContextTax.Core.Tests` — xUnit tests for Core.

## Dependency direction
`Cli → Core ← Web`. Core depends on nothing else in the solution. Keep it that way.

## Build configuration (shared)
- `global.json` pins the .NET 10 SDK.
- `Directory.Build.props` — nullable, implicit usings, latest C#, warnings-as-errors,
  analyzers. (`TargetFramework` is set per project.)
- `Directory.Packages.props` — Central Package Management (versions in one place).
- `.editorconfig` — style, enforced by `dotnet format` in CI.

## Where things live
- Product concept → `product-brief.md`
- Decisions → `decisions/`
- Status / roadmap → `roadmap.md`
- Conventions → `conventions.md`

## Not yet built
The measurement engine itself (a `count_tokens` client, lifecycle measurement, the
strategy harness) is intentionally absent — this is the foundation. See `roadmap.md`.
