# ContextTax

**Measure the context-window "tax" an MCP server or tool imposes on an AI agent** —
schema bloat *and* response bloat, across the full token lifecycle, with ground-truth
accuracy (Anthropic `count_tokens` + real `usage`, not `tiktoken`).

> Status: **early / WIP.** This repository currently contains the project foundation
> (a .NET 10 skeleton + tooling). The measurement engine is in progress.

## Why
A stack of MCP servers can burn 100–200K tokens of an agent's context window before
the user says a word. ContextTax quantifies that cost per server — and reports it in
two currencies: dollars (API) and % of context / limits (subscription). The headline
metric: **% of context window wasted before useful work begins.**

## Build & test
```
dotnet build
dotnet test
```

## Layout
- `src/ContextTax.Core` — measurement engine (library)
- `src/ContextTax.Cli` — report-card CLI
- `src/ContextTax.Web` — dashboard (later)
- `tests/` — tests

## License
MIT — see [LICENSE](LICENSE).
