# 0006 — Lifecycle measurement via a typed CountInput snapshot

- **Status:** Accepted (2026-06-06)

## Context
SP4 measures the full token lifecycle — call overhead + tool-**response** bloat + how cost
accumulates across a multi-turn session — not just the static schema snapshot (SP2). That
means counting real session **messages** (assistant `tool_use`, user `tool_result`), not
only tools, and doing it over the **same** `ITokenCounter` seam so both modes (ground-truth
+ `--estimate`) keep working for free. The source of truth must stay deterministic and
keyless-capable; a live agent run (stochastic, key-gated) is deliberately out of scope.

## Decision
- **Input = a recorded transcript** (Anthropic messages with `tool_use`/`tool_result`),
  parsed into domain models in a new `Transcript/` folder. Faithful to the real request →
  ground-truth accuracy; usually captured, not authored.
- **Grow the seam via a typed `CountInput { Tools?, Messages? }` snapshot**, not a growing
  list of optional `CountAsync` parameters: `ITokenCounter.CountAsync(model, CountInput)`.
  Message content uses typed, `[JsonPolymorphic]` wire blocks (text / tool_use /
  tool_result) so it serializes to the **exact** Anthropic shape — accuracy asserted at the
  wire level.
- **Measure by marginal delta along the message axis** (ADR 0004, applied to the turn
  axis): per-message snapshots give each turn's call (`tool_use`) vs response
  (`tool_result`) tokens and the cumulative context. Turn pairing is **positional** in v1
  (not matched by `tool_use_id`); first `tool_use` in a message wins.
- **Token-and-%-window only; no `$`/cache.** The headline metric (% of window) is
  cache-independent — cached tokens still occupy the window — and a cache/dollar model needs
  live `usage`, so it is out of scope. The static schema report keeps its `$`.

## Consequences
- The whole lifecycle is measurable deterministically, keyless (`--estimate`) **or**
  ground-truth — the wedge other tools miss (they price only the schema snapshot). On the
  sample, one tool response is ~4× the entire server's schema.
- The typed snapshot keeps the seam clean and extensible (a `System` prompt later is one
  field), at the one-time cost of touching every counter/caller.
- Honesty caveat (as ADR 0004): Σ per-turn `added` ≈ cumulative growth but need not match
  to the token (boundary effects); the cumulative snapshot is authoritative.
- Positional pairing + first-`tool_use`-wins are documented v1 limitations that don't affect
  the % -window metric. `tool_use_id` matching is a later refinement.
