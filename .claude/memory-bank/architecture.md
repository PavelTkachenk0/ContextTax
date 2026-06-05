# Architecture

## Solution layout
- `src/ContextTax.Core` — measurement engine (library). No UI; the only I/O is the
  Anthropic HTTP call. Pure and testable. Organized into `Mcp/` (tool ingestion),
  `Counting/` (the ground-truth token-counting seam), and `Measurement/` (cost
  orchestration + report model). Lifecycle and strategy-comparison logic land here in
  later sub-projects.
- `src/ContextTax.Cli` — console entry point (Spectre.Console.Cli). Thin: parse args,
  call Core, render the report card (table or `--json`). Depends on Core.
- `src/ContextTax.Web` — ASP.NET Core report / dashboard host. Thin: serve results
  from Core. Depends on Core. (Web tech — Blazor vs static vs API+SPA — decided in the
  web sub-project.)
- `tests/ContextTax.Core.Tests` — xUnit tests for Core **and** the CLI renderer (the
  test project references the CLI to test `ReportRenderer.RenderJson`).

## Dependency direction
`Cli → Core ← Web`. Core depends on nothing else in the solution. Keep it that way.
Spectre is a CLI-only dependency — Core stays free of UI / framework packages.

## Core internals (built in SP2)
- **`Mcp/`** — `ToolsJsonLoader` parses a tools-JSON document (MCP `tools/list` shape or
  a bare array) → `IReadOnlyList<McpTool>`; `ToolsJsonException` for parse errors.
- **`Counting/`** — the token-counting seam, SRP-split so transport / serialization /
  mapping / domain stay in distinct units:
  - `ITokenCounter` — the abstraction (`CountAsync(model, tools?)`); the only seam the
    measurer knows. Lets tests run offline and future counters (e.g. an offline
    estimator) drop in.
  - `AnthropicTokenCounter` — implements `ITokenCounter`: map → serialize → POST → parse.
  - `AnthropicCountTokensClient` — HTTP transport only (POST the payload, retry once on
    429, throw on non-success). The single piece of network code in Core.
  - `CountTokensRequest` / `CountTokensResponse` (+ message / tool records) — pure wire
    DTOs (internal, data-only).
  - `CountTokensRequestMapper` — `{Target}Mapper.Map(model, tools)`: domain `McpTool` →
    wire DTO.
  - `CountTokensJson` — shared `JsonSerializerOptions` (snake_case + ignore-null), so
    `inputSchema` → `input_schema` is a policy rename, not hand-built JSON.
  - `TokenCountException` — typed error carrying the HTTP status.
- **`Measurement/`** — `SchemaCostMeasurer` (pure orchestration over `ITokenCounter`; the
  marginal-delta method — ADR 0004) → `SchemaCostReport` (+ `ToolCost`).
  `MeasurementOptions` + `Defaults` hold model / window / price (values that drift, kept
  in one place).

## CLI (built in SP2)
- `MeasureCommand` (Spectre `AsyncCommand`) — `measure --tools <path> [--model] [--window]
  [--price] [--json]`: load tools → measure → render. Friendly stderr + non-zero exit on
  a missing key / bad JSON / API error.
- `ReportRenderer` — renders a `SchemaCostReport` as a Spectre table **or** JSON.
- `Program.cs` — builds the Spectre `CommandApp`.

## Build configuration (shared)
- `global.json` pins the .NET 10 SDK.
- `Directory.Build.props` — nullable, implicit usings, latest C#, warnings-as-errors,
  analyzers. (`TargetFramework` is set per project.)
- `Directory.Packages.props` — Central Package Management (versions in one place);
  Spectre.Console(.Cli) added in SP2.
- `.editorconfig` — style, enforced by `dotnet format` in CI.

## Where things live
- Product concept → `product-brief.md`
- Decisions → `decisions/`
- Status / roadmap → `roadmap.md`
- Conventions → `conventions.md`

## Not yet built
Live MCP ingestion (spawn / handshake / `tools/list`), response-bloat + full-lifecycle
measurement, the strategy-comparison harness, subscription budget mode, and the web
dashboard. Next up is the offline `--estimate` counter (a second `ITokenCounter`). See
`roadmap.md`.
