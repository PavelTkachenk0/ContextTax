# 0012 — Paired-turn marginal delta (count_tokens validity)

- **Status:** Accepted (2026-06-07). Amends the marginal-delta method of ADR 0006 (`session`) and ADR 0010 (`response`).

## Context
Dogfooding the **ground-truth** path with a funded API account (pre-launch) surfaced a real bug: both
the `response` (ADR 0010) and `session` (ADR 0006) measurers build **message prefixes that can end at an
assistant `tool_use`** — a dangling `tool_use` with no following `tool_result`. Anthropic `count_tokens`
**rejects** that (HTTP 400: *"tool_use ids were found without tool_result blocks immediately after"*).

- `response`: the **baseline** count was `[user("."), assistant(tool_use)]` — dangling.
- `session`: each per-message **prefix** `messages.Take(i+1)` ending at a `tool_use` is dangling.

The offline **o200k estimate never validates** message structure, so it always "worked" — and the
maintainer was on `--estimate` ($0 API balance). Net effect: **ground-truth lifecycle measurement
(`session` + `response`) had never actually run against the real API** until a funded key was used. The
pre-launch dogfood caught it before the article shipped — fitting, for a tool whose whole thesis is
ground-truth.

## Decision
**Keep every `tool_use` paired with a `tool_result` in every `count_tokens` request.** A shared
`Measurement/ToolResultPadding` helper pads a sequence that ends at an unmatched `tool_use` with a
trailing **empty** `tool_result` (matching `tool_use_id`s, `content: ""`). The empty content is constant,
so it **cancels in the marginal delta** (it only shifts the constant block framing, never the payload).

- `response`: baseline = `PadDanglingToolUse([user, tool_use])` ⇒ `[user, tool_use, tool_result("")]`;
  with-result = `[user, tool_use, tool_result(payload)]`. Delta = the payload's marginal tokens.
- `session`: each prefix is run through `PadDanglingToolUse` before counting; the constant empty-result
  framing is absorbed into that turn's **call** delta (call +framing, response = content-only). The pair
  total and the cumulative/peak are unchanged.
- **Confirmed live:** an empty `tool_result` is accepted by `count_tokens` (`✓ GROUND TRUTH`); the
  formerly-failing `response` on a real captured payload now succeeds.

A **regression guard** (`Never_sends_a_dangling_tool_use_to_the_counter`, both measurers) asserts no
issued `CountInput` ends at an unmatched `tool_use` — catching the class of bug without a live call.

## Consequences
- Ground-truth `response` and `session` work. Estimate numbers are unchanged (o200k never validated).
- The `response` figure is now the payload **content's** marginal cost (the constant `tool_result`
  framing cancels) — a few tokens below the previous (never-valid) figure; negligible for real responses.
- **Observed (worth noting, not load-bearing):** o200k vs `count_tokens` differs by content type, in
  **both** directions — tool **schemas**: o200k *undercounts* Claude by ~16–43%; a large accessibility-tree
  **response**: o200k *overcounts* by ~7% (41,696 est vs 38,831 truth). Estimates are approximate either
  way — a reason to prefer ground truth for any cited number. (README finding scoped to schemas.)
- Orphan `tool_result` (a result with no preceding `tool_use`) remains a documented v1 `session`
  limitation; `count_tokens` may also reject it, but it is malformed input out of scope here.
