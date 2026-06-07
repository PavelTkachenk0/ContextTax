# Roadmap

## Current status
**Sub-projects 1–6: ✅ complete and merged to `main` (CI green).**
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
- **SP4 — Response-bloat + full-lifecycle (the wedge):** measures what tool *responses*
  dump into context and how cost accumulates across a multi-turn session, from a recorded
  Anthropic transcript (`tool_use`/`tool_result`). New `session` CLI command (per-turn
  call/response/cumulative table + response-bloat headline + `--json`), over the **same**
  `ITokenCounter` seam (grown to a typed `CountInput`), so both modes work. Token-and-%-
  window only (no `$`/cache — cache needs live `usage`). See `architecture.md` and ADR 0006.
- **SP5 — Live MCP ingestion:** `measure`/`session` pull `tools/list` from a **running** MCP
  server (`--server <name>` from layered config, or `--url` + `--header` ad-hoc; stdio **and**
  HTTP) via the official `ModelContextProtocol` SDK behind a new `IToolSource` seam — no more
  hand-built tools-JSON. Adds a `servers` command (list configured servers, no connection)
  and shared CLI helpers (`ToolSourceResolver`, `CounterFactory`). Read-only (`initialize` +
  `tools/list` only); secret-safe (header/env values never surfaced); the live test is gated
  out of CI via `CONTEXTTAX_LIVE_TESTS`. See `architecture.md` and ADR 0007.
- **SP6 — Interactive CLI UX:** running `contexttax` with no args launches a Spectre **menu-loop**
  (Measure / Session / List servers / Quit) with guided prompts (source / server-from-config /
  mode), on the existing Spectre dep (no new dependency); the flag commands are unchanged. A shared
  **`MeasurementRunner`** (extracted resolve→count→measure core + typed `RunResult`) now backs both
  the flag commands and the interactive mode (de-dup). Every prompt is cancellable back to the menu;
  a failed action never crashes the loop. See `architecture.md` and ADR 0008.

## Decomposition
1. **Repository Foundation** — ✅ done (SP1).
2. **Schema-cost engine + CLI report card** — ✅ done (SP2). Static schema-token cost
   via `count_tokens` (ground truth) plus the `measure` report card.
3. **Offline `--estimate` mode** — ✅ done (SP3). Keyless approximate counting via the
   o200k_base proxy.
4. **Response-bloat + full-lifecycle** measurement (the wedge) — ✅ done (SP4). Per-turn
   call/response/amortization from a recorded transcript; the `session` command.
5. **Live MCP ingestion** — ✅ done (SP5). Pull `tools/list` from a running server
   (`--server`/`--url`, stdio + HTTP) via the official SDK behind `IToolSource`; the `servers`
   command; shared `ToolSourceResolver`/`CounterFactory`.
6. **Interactive CLI UX** — ✅ done (SP6). A Spectre menu-loop (default command) over the shared
   `MeasurementRunner`; flag commands unchanged.
7. **Web dashboard / leaderboard.**
8. **Strategy comparison harness** — static / tool-search / dynamic / progressive /
   code mode; reproducible, with variance.

## Deferred / open
- **Estimate calibration** — calibrate the o200k_base proxy against ground truth (a
  correction factor) once funded-API access is available. Out of scope for SP3 by design.
- **Claude Code hooks** — revisit (maintainer flagged). Candidate: a commit-guard hook
  complementing CI.
- **Web tech** — Blazor vs static-HTML generator vs API+SPA — decide in the web
  sub-project.
- **Cross-model support** — per-provider token counting beyond Anthropic.
- **CI vs preview SDK** — CI installs the latest 10.0 SDK via `dotnet-quality:
  preview` (resilient to the exact-preview pin; `global.json`'s `rollForward:
  latestFeature` accepts it). Still preferred later: switch `global.json` to the GA
  pin once .NET 10 GA is installed locally.
- **Model → window/price table — considered & DROPPED (2026-06-07).** No reliable auto-source
  (Anthropic `/v1/models` has no window/price; pricing is web-only) → only a stale manual table;
  value is mere convenience (`--window`/`--price` already work). Don't re-propose without an auto-source.
- **Packaging** — ship `contexttax` as a .NET global tool (`PackAsTool` + `dotnet pack`)
  and/or a `dotnet publish` single-file binary, so it runs without `dotnet run`.
- **Command-level CLI tests** — exit-code coverage for `MeasureCommand`/`SessionCommand`
  (today covered only via the loaders/measurer).

## Next
SP6 (interactive CLI UX) is done. Agreed remaining order (brainstorm → spec → plan → implement →
`/sync-memory` each):
- **Web dashboard / leaderboard** — surface the measurements (web tech TBD). **Next up.**
- **Strategy comparison harness** — static / tool-search / dynamic / progressive / code mode.
- **Packaging** — ship `contexttax` as a .NET global tool / single-file publish (likely a later session).
