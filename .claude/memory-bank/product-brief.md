# ContextTax — Product Brief

## One-liner
ContextTax measures the **tax you pay, in context window, for every tool / MCP
server you attach to an agent — before you have done a single useful thing.**

## Why this matters
A modern agent loads tool/server definitions into its context before the user says
a word. A single large MCP server can cost tens of thousands of tokens just to
declare its tools; a stack of 5–10 servers can burn **100–200K tokens** of the
window up front. Every bloated token is one unavailable for real work — and under a
subscription (no per-token billing) it directly shortens how much useful work fits
in a session. **Context is the one scarce resource shared by API and subscription
users alike.**

## What we measure — the full token lifecycle
Most tools price only the *definitions*. ContextTax measures the whole life of a
token through an agent session:

1. **Schema load** — tokens to declare the server's tools (deterministic).
2. **Call overhead** — tokens per tool invocation.
3. **Response tokens** — how much each tool *response* dumps into context. **This is
   the under-measured one** — responses can be enormous and fly straight into the
   window.
4. **Amortization** — how those costs spread across a multi-turn agentic session.

## The wedge (what existing tools miss)
1. **Response bloat**, not only schema bloat.
2. **Full lifecycle**, not a single snapshot.
3. **Neutral, reproducible comparison of optimization strategies** — static /
   tool-search / dynamic toolsets / progressive disclosure / code mode — on the same
   servers and tasks, so results can be *cited* instead of trusted on a vendor's word.
4. **Ground-truth accuracy** (below).

## Ground-truth accuracy (the credibility moat)
- Count tokens with Anthropic's **`count_tokens`** endpoint — same inputs (system +
  tools + messages) as a real request, so the number matches billing. **Not**
  `tiktoken` (wrong tokenizer for Claude).
- Read real **`usage`** from inference responses: `input_tokens`, `output_tokens`,
  and **cache** tokens (`cache_creation_input_tokens` / `cache_read_input_tokens`) —
  caching is priced differently; ignoring it makes cost numbers lie.
- **Tokenizers are model-specific.** For cross-model comparison, use each provider's
  own counting endpoint — never one tokenizer for all.
- **Static vs dynamic.** Schema cost is deterministic (no model call needed).
  Task-running metrics are stochastic → run multiple seeds and report the
  distribution / variance, not a single number.

## Two reporting currencies
Same tokens, two lenses — most tools show only dollars:
- **Dollar mode (API):** tokens → $. Pitfall: input above 200K tokens is billed at a
  **higher rate** (e.g. a doubled input rate) — easy to miss, makes cost wrong.
- **Budget mode (subscription):** tokens → **% of context window** + **% of the
  5-hour window** (~220K on Max 20x) + **% of the weekly cap**. No per-token billing
  exists; the ceiling is the limit.
- **Universal central metric (payment-agnostic): % of context window wasted before
  useful work begins.** This is the headline number on a server's report card.

## Auth note (for the measurement layer)
Subscription auth (OAuth, `sk-ant-oat…`) and API key (`sk-ant-api…`) are different
entities. The `usage` object is available either way (the truth about real
consumption). The static `count_tokens` endpoint needs an API key **on a funded account** (balance
> $0): it is **not billed** (doesn't consume credits), but access still requires a
positive balance — "free" means it doesn't *cost* per call, not that any key works. A
subscription's $0 API balance is gated → see ADR 0005 for the keyless `--estimate`
fallback.

## Competitive landscape & positioning
- **Static schema counters** (e.g. MCP Token Counter, AgiFlow token-usage-metrics) —
  measure definition bloat only.
- **Agentic task benchmarks** (e.g. MCPBench, MCPAgentBench) — measure end-to-end
  task accuracy + tokens, not the *anatomy* of a server's token cost.
- **Vendor blogs** ("100x", "160x", "85%") — impressive but measured on
  incomparable, private setups.
- **The gap ContextTax fills:** response bloat + full lifecycle + a neutral,
  reproducible strategy benchmark, sold on ground-truth accuracy.

## What "winning" looks like
A neutral, reproducible benchmark that people **cite** — the reference for "how much
context does this MCP server actually cost, and which optimization strategy genuinely
helps."

## Form factor
- `ContextTax.Core` — the measurement engine (library).
- `ContextTax.Cli` — runs a measurement and prints/exports a server **report card**.
- `ContextTax.Web` — a dashboard / leaderboard (later sub-project).
