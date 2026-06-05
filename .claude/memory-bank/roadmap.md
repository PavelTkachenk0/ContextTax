# Roadmap

## Current status
**Sub-projects 1–2: ✅ complete and merged to `main` (CI green).**
- **SP1 — Repository Foundation:** a compiling .NET 10 walking skeleton (Core + CLI +
  Web + Core.Tests, CPM, warnings-as-errors), the AI dev infrastructure (lean
  `CLAUDE.md`, memory bank, `/adr` and `/sync-memory`), public-repo hygiene, and CI.
- **SP2 — Schema-cost engine + `measure` CLI:** the first real product capability.
  Ground-truth static schema-token cost via Anthropic `count_tokens` (marginal-delta
  method), rendered as a Spectre report card (total · per-tool · % window · $) with
  `--json`. Network is isolated behind `ITokenCounter`; the live API path is covered by
  a key-gated integration test (skips in CI). See `architecture.md` for the components
  and ADR 0004 for the measurement method.

## Decomposition
1. **Repository Foundation** — ✅ done (SP1).
2. **Schema-cost engine + CLI report card** — ✅ done (SP2). Static schema-token cost
   via `count_tokens` (ground truth) plus the `measure` report card. (The earlier
   separate "engine" and "CLI report card" items shipped together.)
3. **Offline `--estimate` mode** — keyless approximate counting so ContextTax works
   without a funded API key. **← Next.**
4. **Response-bloat + full-lifecycle** measurement (the wedge).
5. **Strategy comparison harness** — static / tool-search / dynamic / progressive /
   code mode; reproducible, with variance.
6. **Web dashboard / leaderboard.**

## Deferred / open
- **Claude Code hooks** — revisit (maintainer flagged). Candidate: a commit-guard hook
  complementing CI.
- **Interactive CLI UX** — a richer Spectre prompt / menu / live-progress mode (Spectre
  is already a dependency). Slot as its own small sub-project; brainstorm when chosen.
- **Web tech** — Blazor vs static-HTML generator vs API+SPA — decide in the web
  sub-project.
- **Cross-model support** — per-provider token counting beyond Anthropic.
- **CI vs preview SDK** — CI installs the latest 10.0 SDK via `dotnet-quality:
  preview` (resilient to the exact-preview pin; `global.json`'s `rollForward:
  latestFeature` accepts it). Still preferred later: switch `global.json` to the GA
  pin once .NET 10 GA is installed locally.

## Next
**Sub-project 3 — offline `--estimate` mode.** Run the spec → plan → implement →
`/sync-memory` cycle. Design direction: ground-truth when a working key is present;
`--estimate` is keyless and available to everyone; never silently approximate (truth and
estimate stay distinct, and the estimate is labelled "≈ approximate"); on a key/balance
error the friendly message points to `--estimate`. Lead approximation candidate: a
tiktoken-style proxy via `Microsoft.ML.Tokenizers` (o200k_base) behind a new
`ITokenCounter` — lock the final choice in the SP3 brainstorm.
