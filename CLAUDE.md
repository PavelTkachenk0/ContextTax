# ContextTax

Measures the **context-window tax** an MCP server / tool imposes on an agent —
schema bloat *and* response bloat, across the full token lifecycle — with
ground-truth accuracy. Full concept: `.claude/memory-bank/product-brief.md`.

## Hard rules (do not break)
- **Public repo.** Never commit secrets, API keys, or personal absolute paths.
  The Anthropic key is read from `ANTHROPIC_API_KEY` (env) or `dotnet user-secrets`
  — never from a committed file.
- **Ground-truth tokens.** Count with Anthropic `count_tokens` + real `usage`
  (incl. cache tokens). Never estimate Claude tokens with `tiktoken`.
- **Internal process docs are local-only.** `docs/superpowers/` is gitignored.

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Format check: `dotnet format --verify-no-changes`
- Run CLI: `dotnet run --project src/ContextTax.Cli`
- Run web: `dotnet run --project src/ContextTax.Web`

## Map (load on demand)
- Concept / why → `.claude/memory-bank/product-brief.md`
- Architecture / where things live → `.claude/memory-bank/architecture.md`
- Conventions (code/test/commit/hygiene) → `.claude/memory-bank/conventions.md`
- Why we chose X → `.claude/memory-bank/decisions/`
- Status / what's next → `.claude/memory-bank/roadmap.md`
- Specs & plans (local) → `docs/superpowers/`

## Workflow loop
brainstorm → spec → plan → implement → **`/sync-memory`** (fold the plan's outcomes
back into the memory bank). TDD per `superpowers:test-driven-development`.
