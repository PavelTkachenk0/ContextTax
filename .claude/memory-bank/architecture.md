# Architecture

## Solution layout
- `src/ContextTax.Core` — measurement engine (library). No UI. The only *network* I/O is
  the Anthropic HTTP call (ground-truth path); the offline estimator runs in-process.
  Organized into `Mcp/` (tool ingestion), `Transcript/` (recorded-session ingestion),
  `Counting/` (the token-counting seam — the ground-truth and the offline counters), and
  `Measurement/` (cost orchestration + report model — schema **and** session). Strategy-
  comparison logic lands here in a later sub-project.
- `src/ContextTax.Cli` — console entry point (Spectre.Console.Cli). Thin: parse args,
  call Core, render the report card (table or `--json`). Depends on Core.
- `src/ContextTax.Web` — ASP.NET Core report / dashboard host. Thin: serve results
  from Core. Depends on Core. (Web tech — Blazor vs static vs API+SPA — decided in the
  web sub-project.)
- `tests/ContextTax.Core.Tests` — xUnit tests for Core **and** the CLI renderer (the
  test project references the CLI to test `ReportRenderer`/`SessionReportRenderer`).

## Dependency direction
`Cli → Core ← Web`. Core depends on no other *project* in the solution. Its only runtime
NuGet deps are the o200k_base tokenizer packages (computation, not UI/framework); Spectre
stays CLI-only.

## Core internals (built in SP2–SP4)
- **`Mcp/`** — `ToolsJsonLoader` parses a tools-JSON document (MCP `tools/list` shape or
  a bare array) → `IReadOnlyList<McpTool>`; `ToolsJsonException` for parse errors.
  (`LoadArray(JsonArray)` is reused by the transcript loader for embedded tools.)
- **`Transcript/`** (SP4) — recorded-session ingestion: `TranscriptLoader` parses an
  Anthropic-messages document (bare array or `{ tools?, messages }`) → `SessionTranscript`
  (`Tools` + `Messages`); domain models `TranscriptMessage` + `ContentBlock`
  (`TextBlock` / `ToolUseBlock` / `ToolResultBlock`); `TranscriptException` for parse errors.
- **`Counting/`** — the token-counting seam, SRP-split so transport / serialization /
  mapping / domain stay in distinct units:
  - `ITokenCounter` — the abstraction (`Mode`, `Label`, `CountAsync(model, CountInput)`); each
    counter **declares its own provenance** (mode + label). The only seam the measurer knows.
  - `CountInput` (SP4) — typed request snapshot `{ Tools?, Messages? }` with `Empty` /
    `ForTools` factories. The seam grew from tools-only to tools **and** session messages via
    this one typed input (not accreting optional params). See ADR 0006.
  - `AnthropicTokenCounter` — ground-truth `ITokenCounter`: map → serialize → POST → parse.
  - `AnthropicCountTokensClient` — HTTP transport only (POST, retry once on 429, throw on
    non-success). The single piece of network code in Core.
  - `EstimateTokenCounter` — offline, keyless `ITokenCounter` (SP3). Tokenizes the **same**
    wire payload the API path sends (reuses `CountTokensRequestMapper` + `CountTokensJson`)
    with the embedded o200k_base tokenizer; `Mode = Estimate`. See ADR 0005.
  - `CountTokensRequest` / `CountTokensResponse` (+ message / tool / content-block records)
    — pure wire DTOs (internal, data-only). Content blocks (text / tool_use / tool_result)
    serialize via `[JsonPolymorphic]` to the exact Anthropic shape (SP4).
  - `CountTokensRequestMapper` — `{Target}Mapper.Map(model, CountInput)`: domain `McpTool` +
    `TranscriptMessage` → wire DTO. Shared by both counters.
  - `CountTokensJson` — shared `JsonSerializerOptions` (snake_case + ignore-null).
  - `TokenCountException` — typed error carrying the HTTP status.
- **`Measurement/`** — `MeasurementMode { GroundTruth | Estimate }`; `SchemaCostMeasurer`
  (pure marginal-delta over **any** `ITokenCounter`, ADR 0004) → `SchemaCostReport`
  (+ `ToolCost`), which now also carries `Mode` + `CounterLabel` — provenance that travels
  into `--json`. `MeasurementOptions` + `Defaults` hold model / window / price.
  **SP4:** `SessionCostMeasurer` (pure marginal-delta along the message axis, ADR 0006) →
  `SessionCostReport` (+ `TurnCost`): per-turn call/response split, cumulative context, peak,
  response-bloat headline ratios, same `Mode`/`CounterLabel` provenance — tokens + % window,
  no `$`.

## CLI (built in SP2–SP4)
- `MeasureCommand` — `measure --tools <path> [--model] [--window] [--price] [--json] [--estimate]`.
  `--estimate` selects the offline counter (keyless, no network); without it the
  ground-truth path requires a key, and the no-key error points the user at `--estimate`.
- `ReportRenderer` — a **mode-aware** Spectre card (a `✓ GROUND TRUTH` / `≈ ESTIMATE` badge,
  `~`-prefixed approximate numbers, an estimate disclaimer footer) **or** JSON (emits `Mode`
  as a string so pipes/CI can tell estimate from truth).
- `SessionCommand` (SP4) — `session --transcript <path> [--tools] [--model] [--window]
  [--json] [--estimate]` (no `--price`). Loads a transcript (+tools), runs
  `SessionCostMeasurer`, renders. Mirrors `MeasureCommand` (counter selection, exit codes).
- `SessionReportRenderer` (SP4) — mode-aware per-turn card + JSON; sibling of `ReportRenderer`.
- `Program.cs` — builds the Spectre `CommandApp` (registers `measure` + `session`).

## Build configuration (shared)
- `global.json` pins the .NET 10 SDK.
- `Directory.Build.props` — nullable, implicit usings, latest C#, warnings-as-errors,
  analyzers. (`TargetFramework` is set per project.)
- `Directory.Packages.props` — Central Package Management: Spectre (SP2) and the o200k_base
  tokenizer packages (`Microsoft.ML.Tokenizers` + `…Data.O200kBase`, SP3).
  `CentralPackageTransitivePinningEnabled` pins a patched `Microsoft.Bcl.Memory` (a
  vulnerable transitive of the tokenizer data package).
- `.editorconfig` — style, enforced by `dotnet format` in CI.

## Where things live
- Product concept → `product-brief.md`
- Decisions → `decisions/`
- Status / roadmap → `roadmap.md`
- Conventions → `conventions.md`

## Not yet built
Live MCP ingestion (spawn / handshake / `tools/list`), the strategy-comparison harness,
subscription budget mode, and the web dashboard. Next up is **live MCP ingestion** vs the
strategy harness (decision pending). See `roadmap.md`.
