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
- **SP7 — Web dashboard / leaderboard:** built & working (local ASP.NET Razor Pages — context-tax
  leaderboard + per-server report card, editorial/Anthropic design, empty-state, gitignored local
  dataset). **Frozen in branch `feat/web-dashboard` — NOT merged.** Mid-build the maintainer judged a
  local "pretty render of measurements" weak next to the CLI (*one great CLI beats two half-finished
  clients*); de-prioritised in favour of CLI quality + the response-measurement feature. Spec/plan
  local-only; revisit after response-measurement + packaging.
- **SP8 — CLI polish:** ✅ merged (#3). Locale-independent numbers (`InvariantCulture` global in
  `Program` + explicit in renderers — kills comma-locale `0,6`), severity-coloured tax/peak %
  (green/yellow/red), ASCII offender bars, short flag aliases (`-s/-e/-t/…`, session `-f`), `--help`
  examples. Cli only; Core untouched; `--json` byte-identical. See `architecture.md` + ADR 0009.
- **SP9 — Automated response measurement:** ✅ merged (#4). New `response` command — count a captured
  tool response's tokens (+ % window) and **diff before/after** an optimisation (`response before -d
  after` → "saved N tok (−X%)"), without a hand-built transcript. Capture-only input: a file, piped
  stdin, or the macOS clipboard (`-C`/`pbpaste`). Cost = marginal delta of a synthetic `tool_use →
  tool_result` turn over the **same** `ITokenCounter` (ground-truth + estimate free; zero counting-layer
  change). Read-only invariant (ADR 0007) intact — no `tools/call`; live `--call` deferred. Mode-aware
  card (before/after bars + coloured headline) + `--json` + interactive menu entry. See `architecture.md` + ADR 0010.
- **SP10 — Packaging + public launch:** ✅ done & **shipped (v1.0.0 → v1.0.1)**. `contexttax` now runs as a
  **self-contained, single-file binary** on **GitHub Releases** (no `dotnet run`, no .NET on the user's box)
  for `osx-arm64`/`osx-x64`/`linux-x64`/`win-x64` — built by a tagged `release.yml` (one ubuntu job,
  cross-compiled; `softprops/action-gh-release`, stable asset names), installable via one-line
  `install.sh`/`install.ps1`. No NuGet, no trim/AOT (compression instead). `--version`,
  `InvariantGlobalization`. Editorial README (SVG hero light/dark via `<picture>` + terminal demo +
  Downloads + Reproduce + the **"55K" reconciliation** + a **response-bloat** section) and a PR-driven
  `LEADERBOARD.md`. **Repo is public.** ADR 0011.
  - **Ground-truth verified** (funded API): `count_tokens` headline — **GitHub MCP 10,928 tok / 5.5%** of a
    200K window (default toolset; 20,404 / 10.2% all toolsets), beside the o200k estimate. **Calibration
    data:** o200k vs `count_tokens` **cuts both ways** — tool schemas undercount 16–43%, while a large
    response (Playwright `browser_snapshot` = 38,831 tok / 19.4%) overcounts ~7%.
  - **Bugfix (ADR 0012):** dogfooding ground-truth caught that `response`/`session` built message prefixes
    ending at a **dangling `tool_use`** → `count_tokens` HTTP 400 (masked by the non-validating o200k
    estimate — the maintainer had only ever run `--estimate`). Fixed by padding a dangling `tool_use` with
    a synthetic **empty** `tool_result` (`ToolResultPadding`, shared by both measurers); regression guard
    added; confirmed live. Shipped as **v1.0.1**.

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
7. **Web dashboard / leaderboard** — ⏸ built & **frozen** in a branch (not merged); de-prioritised
   for CLI quality. Revisit later.
8. **CLI polish** — ✅ done (SP8). Invariant numbers, severity colour, offender bars, flag aliases,
   `--help` examples. Merged (#3).
9. **Automated response measurement** — ✅ done (SP9). The `response` command: count a captured
   response's tokens + % window and diff before/after, from a file / piped stdin / macOS clipboard;
   marginal-delta synthetic turn; read-only intact (live `--call` deferred). Merged (#4).
10. **Packaging + public launch** — ✅ done (SP10). Self-contained cross-platform binaries on GitHub
    Releases (no NuGet, no .NET runtime), tagged `release.yml`, one-line installers, release-ready README
    + real ground-truth loud number + `LEADERBOARD.md`; ground-truth path verified; the dangling-`tool_use`
    bugfix (ADR 0012). Shipped **v1.0.0 → v1.0.1**; repo public.
11. **Strategy comparison harness** — static / tool-search / dynamic / progressive / code; with variance.

## Deferred / open
- **Estimate calibration** — funded-API access now exists; first data shows o200k vs `count_tokens`
  **cuts both ways** (tool schemas −16–43%, a big accessibility-tree response +~7%), so a single
  correction factor won't do — a content-aware calibration is a possible follow-up. (Was out of scope for SP3.)
- **Claude Code hooks** — revisit (maintainer flagged). Candidate: a commit-guard hook
  complementing CI.
- **Web tech — decided (SP7, frozen):** local ASP.NET Razor Pages (not Blazor/SSG/SPA) — runs on
  `localhost`, single .NET stack, gitignored local dataset, empty-state. In `feat/web-dashboard`.
- **Cross-model support** — per-provider token counting beyond Anthropic.
- **CI vs preview SDK** — CI installs the latest 10.0 SDK via `dotnet-quality:
  preview` (resilient to the exact-preview pin; `global.json`'s `rollForward:
  latestFeature` accepts it). Still preferred later: switch `global.json` to the GA
  pin once .NET 10 GA is installed locally.
- **Model → window/price table — considered & DROPPED (2026-06-07).** No reliable auto-source
  (Anthropic `/v1/models` has no window/price; pricing is web-only) → only a stale manual table;
  value is mere convenience (`--window`/`--price` already work). Don't re-propose without an auto-source.
- **Command-level CLI tests** — exit-code coverage for `MeasureCommand`/`SessionCommand`
  (today covered only via the loaders/measurer).

## Next
SP10 (packaging + public launch) shipped **v1.0.0 → v1.0.1**; repo is **public**; the ground-truth path
is verified and the dangling-`tool_use` bug is fixed (ADR 0012). SP7 (web) built but **frozen** in a branch.
Direction (maintainer, 2026-06-08): the code is ready — the **name comes from the launch**.
- **Launch / reputation (the lever).** Write the post (the *real* number vs the mythical "55K" + the
  response-bloat moat) → Show HN / r/LocalLLaMA / r/ClaudeAI. Fill GitHub **About + topics** (manual — the
  PAT can't; text is ready, or connect the Chrome extension and the assistant sets it). Grow `LEADERBOARD.md` via PRs.
- **Live `--call` (own cycle).** The "pick server → tool → invoke → measure the real response" flow. Breaks
  read-only (ADR 0007), unsafe vs mutating server tools — needs an opt-in flag, a new ADR, and a safety
  model (explicit tool+args, never auto-called, a side-effect warning). Reuses the SP5 `IToolSource`.
- **Estimate calibration** — content-aware (o200k cuts both ways; data in hand).
- **Strategy comparison harness (#11)**, **web dashboard** (unfreeze) — later.
