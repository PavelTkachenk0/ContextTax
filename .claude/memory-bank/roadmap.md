# Roadmap

## Current status
**Sub-projects 1–3: ✅ complete and merged to `main` (CI green).**
- **SP1 — Repository Foundation:** a compiling .NET 10 walking skeleton (Core + CLI +
  Web + Core.Tests, CPM, warnings-as-errors), the AI dev infrastructure (lean
  `CLAUDE.md`, memory bank, `/adr` and `/sync-memory`), public-repo hygiene, and CI.
- **SP2 — Schema-cost engine + `measure` CLI:** the first real product capability.
  Ground-truth static schema-token cost via Anthropic `count_tokens` (marginal-delta
  method), rendered as a Spectre report card (total · per-tool · % window · $) with
  `--json`. Network is isolated behind `ITokenCounter`; the live API path is covered by
  a key-gated integration test (skips in CI). See `architecture.md` and ADR 0004.
- **SP3 — Offline `--estimate` mode:** keyless, offline approximate counting via the
  o200k_base tokenizer (`Microsoft.ML.Tokenizers`, embedded vocab). Explicit `--estimate`
  flag (no silent fallback); the measurement mode is carried as provenance
  (`MeasurementMode` + `CounterLabel`) into the report and `--json`. A deliberate,
  clearly-labelled (`≈`) departure from the ground-truth thesis — for the keyless mode
  only. See ADR 0005.

## Decomposition
1. **Repository Foundation** — ✅ done (SP1).
2. **Schema-cost engine + CLI report card** — ✅ done (SP2). Static schema-token cost
   via `count_tokens` (ground truth) plus the `measure` report card.
3. **Offline `--estimate` mode** — ✅ done (SP3). Keyless approximate counting via the
   o200k_base proxy.
4. **Response-bloat + full-lifecycle** measurement (the wedge). **← Next.**
5. **Strategy comparison harness** — static / tool-search / dynamic / progressive /
   code mode; reproducible, with variance.
6. **Web dashboard / leaderboard.**

## Deferred / open
- **Estimate calibration** — calibrate the o200k_base proxy against ground truth (a
  correction factor) once funded-API access is available. Out of scope for SP3 by design.
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
**Sub-project 4 — response-bloat + full-lifecycle measurement** (the wedge): measure what
tool *responses* dump into context, and how costs amortize across a multi-turn agentic
session — not just the static schema snapshot. Run the spec → plan → implement →
`/sync-memory` cycle.
