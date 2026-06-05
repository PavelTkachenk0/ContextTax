# 0005 — Keyless estimate via an o200k_base tokenizer proxy

- **Status:** Accepted (2026-06-05)

## Context
The ground-truth path needs Anthropic `count_tokens`, which requires a funded API
account. A subscription-only user is paywalled out of it, so ContextTax could not run
for them at all. We want a keyless fallback — accepting that it cannot be ground truth.

## Decision
Add `measure --estimate`: an offline, keyless, approximate count via the **o200k_base**
tokenizer (`Microsoft.ML.Tokenizers` + the embedded `…Data.O200kBase` vocab — no network).
It tokenizes the **same wire payload** the API path would send (reusing the count_tokens
mapper + JSON options), so it is the closest possible proxy. The count is **raw, not
calibrated**, and loudly labelled `≈`. The trigger is an **explicit flag** (no silent
fallback); the measurement **mode is provenance** carried in `SchemaCostReport`
(`MeasurementMode` + `CounterLabel`) and in `--json`; each `ITokenCounter` declares its
own mode/label.

## Consequences
- ContextTax now runs end-to-end with no key. Positioning: **API → exact, subscription →
  approximate**.
- A deliberate, clearly-marked departure from ADR 0003 ("ground truth, not tiktoken") —
  **for the keyless mode only**. Truth and estimate stay distinct in the type system, the
  card, and `--json`; they are never silently interchanged.
- The estimate differs from Claude's real tokenizer (non-Claude vocab; Anthropic tool
  overhead). Calibration against ground truth (a correction factor) is possible later and
  is explicitly out of scope now.
