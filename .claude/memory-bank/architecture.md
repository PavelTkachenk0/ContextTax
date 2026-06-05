# Architecture

## Solution layout
- `src/ContextTax.Core` — measurement engine (library). No UI. The only *network* I/O is
  the Anthropic HTTP call (ground-truth path); the offline estimator runs in-process.
  Organized into `Mcp/` (tool ingestion), `Counting/` (the token-counting seam — the
  ground-truth and the offline counters), and `Measurement/` (cost orchestration + report
  model). Lifecycle and strategy-comparison logic land here in later sub-projects.
- `src/ContextTax.Cli` — console entry point (Spectre.Console.Cli). Thin: parse args,
  call Core, render the report card (table or `--json`). Depends on Core.
- `src/ContextTax.Web` — ASP.NET Core report / dashboard host. Thin: serve results
  from Core. Depends on Core. (Web tech — Blazor vs static vs API+SPA — decided in the
  web sub-project.)
- `tests/ContextTax.Core.Tests` — xUnit tests for Core **and** the CLI renderer (the
  test project references the CLI to test `ReportRenderer.RenderJson`).

## Dependency direction
`Cli → Core ← Web`. Core depends on no other *project* in the solution. Its only runtime
NuGet deps are the o200k_base tokenizer packages (computation, not UI/framework); Spectre
stays CLI-only.

## Core internals (built in SP2–SP3)
- **`Mcp/`** — `ToolsJsonLoader` parses a tools-JSON document (MCP `tools/list` shape or
  a bare array) → `IReadOnlyList<McpTool>`; `ToolsJsonException` for parse errors.
- **`Counting/`** — the token-counting seam, SRP-split so transport / serialization /
  mapping / domain stay in distinct units:
  - `ITokenCounter` — the abstraction (`Mode`, `Label`, `CountAsync(model, tools?)`); each
    counter **declares its own provenance** (mode + label). The only seam the measurer knows.
  - `AnthropicTokenCounter` — ground-truth `ITokenCounter`: map → serialize → POST → parse.
  - `AnthropicCountTokensClient` — HTTP transport only (POST, retry once on 429, throw on
    non-success). The single piece of network code in Core.
  - `EstimateTokenCounter` — offline, keyless `ITokenCounter` (SP3). Tokenizes the **same**
    wire payload the API path sends (reuses `CountTokensRequestMapper` + `CountTokensJson`)
    with the embedded o200k_base tokenizer; `Mode = Estimate`. See ADR 0005.
  - `CountTokensRequest` / `CountTokensResponse` (+ message / tool records) — pure wire
    DTOs (internal, data-only).
  - `CountTokensRequestMapper` — `{Target}Mapper.Map(model, tools)`: domain `McpTool` →
    wire DTO. Shared by both counters.
  - `CountTokensJson` — shared `JsonSerializerOptions` (snake_case + ignore-null).
  - `TokenCountException` — typed error carrying the HTTP status.
- **`Measurement/`** — `MeasurementMode { GroundTruth | Estimate }`; `SchemaCostMeasurer`
  (pure marginal-delta over **any** `ITokenCounter`, ADR 0004) → `SchemaCostReport`
  (+ `ToolCost`), which now also carries `Mode` + `CounterLabel` — provenance that travels
  into `--json`. `MeasurementOptions` + `Defaults` hold model / window / price.

## CLI (built in SP2–SP3)
- `MeasureCommand` — `measure --tools <path> [--model] [--window] [--price] [--json] [--estimate]`.
  `--estimate` selects the offline counter (keyless, no network); without it the
  ground-truth path requires a key, and the no-key error points the user at `--estimate`.
- `ReportRenderer` — a **mode-aware** Spectre card (a `✓ GROUND TRUTH` / `≈ ESTIMATE` badge,
  `~`-prefixed approximate numbers, an estimate disclaimer footer) **or** JSON (emits `Mode`
  as a string so pipes/CI can tell estimate from truth).
- `Program.cs` — builds the Spectre `CommandApp`.

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
Live MCP ingestion (spawn / handshake / `tools/list`), response-bloat + full-lifecycle
measurement, the strategy-comparison harness, subscription budget mode, and the web
dashboard. Next up is response-bloat + full-lifecycle. See `roadmap.md`.
