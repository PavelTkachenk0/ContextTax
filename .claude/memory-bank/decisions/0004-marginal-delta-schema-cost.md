# 0004 — Marginal-delta schema-cost measurement

- **Status:** Accepted (2026-06-05)

## Context
We need the token cost a server's tools add to a request. A request has a fixed
baseline (model + a minimal message) before any tools; counting tools naively would
include that baseline and overstate the cost.

## Decision
Measure by **marginal delta** over `count_tokens`, with a minimal request
(`messages: [{ role: "user", content: "." }]`) as the baseline:
- `baseline` = count(no tools)
- `total` = count(all tools) − baseline   ← authoritative
- `perTool[i]` = count(only tool i) − baseline, sorted descending

This is `N + 2` `count_tokens` calls (free, not billed). `count_tokens` returns
`input_tokens`; deltas are clamped at ≥ 0.

## Consequences
- Isolates the tools' real contribution; numbers stay ground-truth and citable.
- **Honesty caveat:** Σ`perTool` ≈ `total` but need not match exactly (tokenizer
  boundary effects). `total` (all tools at once) is authoritative; per-tool numbers are
  each tool's cost *in isolation* — surfaced as such in the report.
- `N + 2` calls is fine now; batching is a possible later optimization for large N.
