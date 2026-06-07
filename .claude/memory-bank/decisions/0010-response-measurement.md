# 0010 — Automated response measurement: capture-only synthetic-turn marginal delta

- **Status:** Accepted (2026-06-07)

## Context
SP4 made the wedge — measuring what a tool *response* dumps into context — but it only landed via the
`session` command, which needs a **hand-built Anthropic transcript**. Too fiddly: *"nobody will assemble
that by hand."* In a real agent session the response you want to measure sits embedded/collapsed inside
the conversation, copy-able as a raw blob. We needed a `measure`-easy flow: count a captured response's
tokens (+ % window) and **diff before/after** an optimisation — the project's wedge made usable.

## Decision
A new **`response` command, capture-only** (approach A, chosen over B = A + live `--call`, and C = live
only — both deferred):

- **Cost = marginal delta of a synthetic turn.** Wrap the payload as a **string** `tool_result` in a
  minimal `user(".") → assistant(tool_use) → user(tool_result)` turn; cost = `count(with result) −
  count(baseline)` over the existing `ITokenCounter` (ADR 0004 on the message axis). The constant prefix
  cancels → the **pure response contribution**. Ground-truth **and** offline estimate both work with
  **zero counting-layer change**. New small `ResponseCostMeasurer` + pure `ResponseDiff.Between` (SRP —
  *not* a reuse of `SessionCostMeasurer`, which carries schema/turn-array baggage a single response
  doesn't need).
- **Payload measured as text** (a string `tool_result.content`), faithful to how MCP results reach the
  model; format-agnostic (works on non-JSON). Caveat: a UI may pretty-print JSON — pipe the raw bytes
  for wire-faithful counts.
- **Read-only invariant (ADR 0007) preserved** — no `tools/call`. The "pick server → tool → invoke →
  measure the real response" flow is **live `--call`**, deliberately **deferred**: it breaks read-only
  and is unsafe against mutating server tools (`query`/`execute`) — its own cycle with an opt-in flag,
  a new ADR, and a safety model (explicit tool+args, never auto-called, side-effect warning).
- **Input: a file, piped stdin, or the macOS clipboard.** stdin is read with **PATH omitted +
  `Console.IsInputRedirected`** (a pipe) — Spectre.Console.Cli rejects a literal `-` positional
  ("Option does not have a name"); the runner still reads stdin via its internal `-`. Clipboard via
  `pbpaste` (new CLI `ClipboardReader`, macOS), single-response only. `ReadInput` guards a pasted blob
  given as a path and **never echoes it** back in the error.
- **Visual parity** with the `measure`/`session` cards (rounded table, `TaxSeverity` colour, before/after
  `TokenBar` bars, a colour-coded `Headline`, ASCII minus) + `--json`; an interactive menu entry with a
  clipboard/file source choice. Tokens + % window only (no `$`/cache — like `session`).

## Consequences
- The wedge is usable: `response before.json -d after.json` → "saved N tok (−X %)". Demoable **keyless**
  on neutral `samples/responses/`; a hermetic e2e runs the real o200k tokenizer over them.
- Core counting layer and the MCP/`IToolSource` path are untouched — a response's cost is independent of
  the tool schema, so `response` needs no tool source (simpler than `measure`/`session`).
- Reading file/stdin + friendly error mapping live in `MeasurementRunner` (new `RunResponseAsync` /
  `RunResponseDeltaAsync` / `RunResponseTextAsync`); the Core measurer/diff stay pure.
- Deferred behind this: **live `--call`** (own cycle), clipboard beyond macOS, clipboard-side diff,
  `$`/cache for responses, and the n-way strategy-comparison harness. **Packaging** (`contexttax` as a
  global tool) is now the front-running next step.
