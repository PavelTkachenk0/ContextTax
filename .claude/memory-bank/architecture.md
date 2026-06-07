# Architecture

## Solution layout
- `src/ContextTax.Core` — measurement engine (library). No UI. Network/process I/O is the
  Anthropic HTTP call (ground-truth path) and live MCP ingestion (`Mcp/LiveToolSource`, the
  SDK); the offline estimator runs in-process. Organized into `Mcp/` (tool ingestion — file
  **and** live server), `Transcript/` (recorded-session ingestion),
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
`Cli → Core ← Web`. Core depends on no other *project* in the solution. Its runtime NuGet
deps are the o200k_base tokenizer packages (computation, not UI/framework) and the
`ModelContextProtocol` client SDK (live ingestion); Spectre stays CLI-only.

## Core internals (built in SP2–SP9)
- **`Mcp/`** — `ToolsJsonLoader` parses a tools-JSON document (MCP `tools/list` shape or
  a bare array) → `IReadOnlyList<McpTool>`; `ToolsJsonException` for parse errors.
  (`LoadArray(JsonArray)` is reused by the transcript loader for embedded tools.)
  **SP5 (live ingestion):** `IToolSource` — the live-source seam (`GetToolsAsync(ct) →
  McpTool[]`, mirroring `ITokenCounter`); `LiveToolSource` — that seam over the official
  `ModelContextProtocol` SDK (build transport from config → `initialize` → `tools/list` →
  map → dispose; stdio **and** HTTP; read-only, never `tools/call`; the only net/process
  code); `McpToolMapper` (`{Target}Mapper.Map`: SDK tool → `McpTool`); `McpServerConfig`
  (pure record) + `McpConfigResolver` (layered `mcpServers` → `List()`/`Resolve(name)`,
  `${ENV}` resolution, no network) + `McpConfigException`. See ADR 0007.
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
  **SP9:** `ResponseCostMeasurer` (pure; wraps a captured payload as a synthetic `user(".") →
  assistant(tool_use) → user(tool_result)` turn and returns the **marginal delta** of the result
  block over the same `ITokenCounter` — the constant prefix cancels, ADR 0010) → `ResponseCostReport`;
  `ResponseDiff.Between(before, after)` (pure combinator) → `ResponseDiffReport` (signed `DeltaTokens`
  + `DeltaPercent`, `null` at `before = 0`). Tokens + % window, no `$`; zero counting-layer change.

## CLI (built in SP2–SP6, polished SP8, response SP9)
- `MeasureCommand` — `measure (--tools <path> | --server <name> | --url <url> [--header "K: V"]…)
  [--config] [--timeout] [--model] [--window] [--price] [--json] [--estimate]`. Tool-source,
  counter selection, and the resolve→count→measure run are delegated to the shared `Support/`
  helpers (`MeasurementRunner` et al., below), so the command stays thin. `--estimate` selects the offline counter (keyless); without it the ground-truth path
  needs a key, and the no-key error points the user at `--estimate`.
- `ReportRenderer` — a **mode-aware** Spectre card (a `✓ GROUND TRUTH` / `≈ ESTIMATE` badge,
  `~`-prefixed approximate numbers, an estimate disclaimer footer) **or** JSON (emits `Mode`
  as a string so pipes/CI can tell estimate from truth).
- `SessionCommand` (SP4; SP5 sources) — `session --transcript <path> (--tools | --server |
  --url [--header …]…) [--config] [--timeout] [--model] [--window] [--json] [--estimate]`
  (no `--price`). Loads a transcript; external tools come from the same `Support/` helpers
  (embedded transcript tools still win). Runs `SessionCostMeasurer`, renders.
- `SessionReportRenderer` (SP4) — mode-aware per-turn card + JSON; sibling of `ReportRenderer`.
- `ServersCommand` (SP5) — `servers [--config] [--json]` lists discovered servers via
  `McpConfigResolver.List()` (no connection); `Rendering/ServersRenderer` renders a table or
  `--json`, emitting header **key names only** (values never surfaced).
- `Support/` (SP5–SP6) — `ToolSourceResolver` resolves `--tools | --server | --url` → `McpTool[]`
  (a `Func<McpServerConfig, IToolSource>` factory is injected so tests use a fake; a malformed
  `--header` → exit 2 without echoing the value); `CounterFactory` selects the counter and owns
  the `HttpClient` lifetime; `McpConfig` builds the layered resolver from `./.mcp.json` +
  `~/.claude.json`. **SP6:** `MeasurementRunner` (+ `RunResult<T>`) is the extracted
  resolve→count→measure core — given a `ToolSourceOptions` + a counter it returns a report **or**
  a friendly `(message, exitCode)`; both the flag commands and the interactive mode call it (one
  code path). Shared by `measure` + `session` (+ interactive).
- `Interactive/` + `Commands/InteractiveCommand` (SP6) — the menu-loop, the **default command**
  when run with no args: a banner + `SelectionPrompt` menu → guided prompts (`InteractivePrompts`:
  source / server-from-config / mode; header values entered masked) → the shared `MeasurementRunner`
  → the existing cards. Every prompt is cancellable back to the menu (`← Back` / blank entry); a
  failed action is shown and returns to the menu (never crashes). No new measurement logic.
- `Program.cs` — sets `InvariantCulture` globally (SP8, locale-independent output) then builds the
  Spectre `CommandApp`; `SetDefaultCommand<InteractiveCommand>()` (no-args → interactive) + registers
  `interactive` + `measure` + `session` + `servers`; `--help` carries usage examples (`AddExample`).
- **SP8 — CLI polish (`Rendering/`):** all card numbers format with `InvariantCulture` (global in
  `Program` *and* explicit per-format, so output is locale-independent and unit-testable). New
  `TaxSeverity` (percent → Low/Medium/High → Spectre colour; thresholds 5/10 %) colours the tax /
  peak-% figures; `TokenBar` renders fixed-width `█/░` offender bars. Every flag gained a short alias
  (`-s/-e/-t/-u/-c/-m/-w/-j/-p/-H`; session `-f` = `--transcript`) with example-bearing descriptions.
  Renderer output is asserted with a `TestConsole` (test-only `Spectre.Console.Testing`). Core untouched.
- **SP9 — response measurement (`response` command):** `ResponseCommand` (`response [PATH] [-d|--delta
  <path>] [-C|--clipboard] [-m] [-w] [-j] [-e]`) — capture-only, thin (counter via `CounterFactory`,
  measure via the runner, render). Input is a file, **piped stdin** (PATH omitted + `IsInputRedirected`
  — Spectre rejects a literal `-` positional), or the **macOS clipboard** via new `Support/ClipboardReader`
  (`pbpaste`; single-response only). `MeasurementRunner` grew `RunResponseAsync` / `RunResponseDeltaAsync`
  / `RunResponseTextAsync` + private `ReadInput` (file/stdin; guards a pasted blob without echoing it).
  `Rendering/ResponseReportRenderer` — mode-aware single / diff cards (rounded table, `TaxSeverity`
  colour, before/after `TokenBar` bars, coloured `Headline`, ASCII minus) + `--json`. Registered in
  `Program.cs` (+ examples); the interactive menu gains a "Measure a captured response" entry with a
  clipboard/file source choice. Neutral `samples/responses/weather.before|after.json` ship for a keyless
  demo. Core untouched. See ADR 0010.

## Build configuration (shared)
- `global.json` pins the .NET 10 SDK.
- `Directory.Build.props` — nullable, implicit usings, latest C#, warnings-as-errors,
  analyzers. (`TargetFramework` is set per project.)
- `Directory.Packages.props` — Central Package Management: Spectre (SP2), the o200k_base
  tokenizer packages (`Microsoft.ML.Tokenizers` + `…Data.O200kBase`, SP3), and
  `ModelContextProtocol` (the client SDK, SP5). `CentralPackageTransitivePinningEnabled` pins
  a patched `Microsoft.Bcl.Memory` (a vulnerable transitive of the tokenizer data package).
  `Spectre.Console.Testing` is a **test-only** dep (SP8) for asserting rendered card output.
- `.editorconfig` — style, enforced by `dotnet format` in CI.
- **Packaging / distribution (SP10):** `ContextTax.Cli` csproj sets `<AssemblyName>contexttax</AssemblyName>`
  + `<InvariantGlobalization>true</…>`; **publish-only** flags (`-r <rid> --self-contained
  -p:PublishSingleFile -p:EnableCompressionInSingleFile`, no trim/AOT) live on the `dotnet publish` line in
  `scripts/publish.sh`, so dev builds stay framework-dependent. A tagged **`.github/workflows/release.yml`**
  runs `publish.sh` → a GitHub Release (4 RIDs + `install.sh`/`install.ps1`, stable asset names); `ci.yml`
  is untouched. `contexttax --version` comes from the assembly informational version. The README front door
  uses committed `assets/*.svg` via `<img>` + `<picture>`. See ADR 0011.
- **Measurement fix (ADR 0012):** `Measurement/ToolResultPadding` pads a dangling `tool_use` with an empty
  `tool_result` so the `response` / `session` marginal-delta requests are valid for `count_tokens` (which
  rejects a dangling `tool_use`; the offline o200k estimate never validated, so the bug hid until a funded key).

## Where things live
- Product concept → `product-brief.md`
- Decisions → `decisions/`
- Status / roadmap → `roadmap.md`
- Conventions → `conventions.md`

## Not yet built
**Live `--call`** (invoke a tool and measure its *real* response — deferred; it breaks read-only and needs
a safety model for mutating server tools), the strategy-comparison harness, and subscription budget mode.
The **web dashboard** is built but **frozen** in branch `feat/web-dashboard` (local ASP.NET Razor Pages —
leaderboard + report card; Dataset/Models/Pages/wwwroot; not merged) — revisited later. Next up: the
**launch** (post + GitHub About/topics) and live `--call` (see `roadmap.md`).
