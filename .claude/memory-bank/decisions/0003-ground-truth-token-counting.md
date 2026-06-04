# 0003 — Ground-truth token counting (not tiktoken)

- **Status:** Accepted (2026-06-04)

## Context
Accuracy is ContextTax's differentiator. `tiktoken` is the wrong tokenizer for Claude
and would produce numbers that disagree with billing.

## Decision
Count Claude tokens with Anthropic's **`count_tokens`** endpoint and read real
**`usage`** (including cache tokens) from inference responses. For cross-model work,
use each provider's own counting endpoint. Treat static (schema) metrics as
deterministic and dynamic (task) metrics as stochastic (multiple seeds + variance).

## Consequences
- Numbers match provider billing → citable.
- A (free) API key is required for the `count_tokens` measurement layer, even under a
  subscription.
