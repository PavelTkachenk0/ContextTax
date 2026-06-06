# 0007 — Live MCP ingestion via the official SDK behind an IToolSource seam

- **Status:** Accepted (2026-06-07)

## Context
Until SP5 the only tool input was a tools-JSON file (`--tools`). SP4 dogfooding exposed the
gap: measuring a real *running* server meant hand-assembling its `tools/list` JSON by hand.
Pulling `tools/list` straight from a live server is what a user reaches for first. Key
insight: live ingestion just yields `IReadOnlyList<McpTool>` — the **same** output as
`ToolsJsonLoader` — so it slots in as an alternative *tool source* and the measurement
engine (`SchemaCostMeasurer`/`SessionCostMeasurer`, `--estimate`/ground-truth, the report)
stays untouched.

## Decision
- **Live ingestion = a new tool source behind an `IToolSource` seam**
  (`GetToolsAsync(ct) → McpTool[]`), mirroring the `ITokenCounter` seam. It is the only
  new network/process code; everything downstream is unchanged.
- **Use the official `ModelContextProtocol` C# SDK (client)** to connect → `initialize` →
  `tools/list`. The canonical client means our `tools/list` matches what a real agent
  loads (credibility), and both transports are built-in; hand-rolling Streamable-HTTP is
  fiddly and a spec-tracking burden. Server/AspNetCore packages are **not** referenced. The
  exact SDK symbols (they drifted across previews) were pinned by a spike against the
  installed version — HTTP is `HttpClientTransport` + `HttpTransportMode.StreamableHttp`.
- **Read-only:** only `initialize` + `tools/list`, **never `tools/call`** → zero side effects.
- **Two transports:** stdio (command/args/env) and HTTP (url/headers).
- **Config:** `--server <name>` resolves from layered `mcpServers` (`./.mcp.json` →
  `~/.claude.json` per-project[cwd] → global; project wins). `--url` (+ repeatable
  `--header "K: V"`) is the ad-hoc fallback. `${ENV}` placeholders resolve from the
  environment; a missing var errors naming the **variable, never the value**.
- **Secret-safe:** header/env values are used only to connect — never logged, printed in
  errors, or emitted to `--json`; `servers` shows header *keys* only; a malformed `--header`
  errors (exit 2) **without echoing the value**.
- **Thin commands:** shared CLI helpers `ToolSourceResolver` (`--tools | --server | --url`
  → `McpTool[]`; a `Func<McpServerConfig, IToolSource>` factory is injected so tests use a
  fake) and `CounterFactory` (counter selection + `HttpClient` ownership), used by both
  `measure` and `session` (rule-of-three reached). New `servers` command lists discovered
  servers (no connection).
- **Gated live test:** the one test that spawns a real server is opt-in via
  `CONTEXTTAX_LIVE_TESTS=1`, so CI stays hermetic — no network, no npm packages pulled. This
  generalizes the existing `ANTHROPIC_API_KEY` gate.

## Consequences
- ContextTax points at a running MCP server directly — no hand-assembled tools-JSON.
  Verified end-to-end: `measure --server <name> --estimate` pulled 13 tools from a live
  reference server, keyless.
- The engine, both counters, and `--estimate`/ground-truth are untouched — the live source
  feeds the same `McpTool[]` the file loader does.
- A new runtime dependency (the SDK) enters `Core`; preview-drift risk is mitigated by the
  symbol-pinning spike plus a clean warnings-as-errors build.
- Exit codes are carried by a typed `ToolSourceException.ExitCode`: usage/config → 2,
  connection/auth/timeout → 1.
- Recorded as out of scope: `tools/call`, live response-bloat (a real agent run), and
  writing/merging MCP config.
