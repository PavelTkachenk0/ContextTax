# ContextTax

**Measure the context-window "tax" an MCP server or tool imposes on an AI agent** —
schema bloat *and* response bloat, across the full token lifecycle. Token counts are
**ground-truth** (Anthropic `count_tokens` + real `usage`) when an API key is present,
and an **offline `≈` estimate** (o200k_base tokenizer, keyless) when it isn't.

> Status: **working — schema-cost measurement shipped.** `contexttax measure` reports a
> server's static schema-token cost today, ground-truth or offline estimate. Response-bloat
> + full-lifecycle measurement, a strategy-comparison harness, and a web dashboard are next.

## Why
A stack of MCP servers can burn 100–200K tokens of an agent's context window before
the user says a word. ContextTax quantifies that cost per server — and reports it in
two currencies: dollars (API) and % of context / limits (subscription). The headline
metric: **% of context window wasted before useful work begins.**

## Measure a server
Point it at a tools-JSON file (an MCP `tools/list` result, or a bare array of tools):

```
# Ground-truth (needs ANTHROPIC_API_KEY on a funded API account):
dotnet run --project src/ContextTax.Cli -- measure --tools samples/tools/filesystem.tools.json

# Offline estimate — keyless, no network, approximate:
dotnet run --project src/ContextTax.Cli -- measure --tools samples/tools/filesystem.tools.json --estimate

# Machine-readable:
dotnet run --project src/ContextTax.Cli -- measure --tools <file> --estimate --json
```

Example (estimate):
```
ContextTax · report card · filesystem            ≈ ESTIMATE
Schema (tools loaded)   ~168 tok
Context tax             ~0.1 % of a 200,000 window
Est. API-equivalent     ~$0.00
Counted with            o200k_base (offline proxy)
```

**Two accuracy modes, never mixed up.** With a funded API key you get exact, citable
numbers (`count_tokens`). Without one, `--estimate` gives a keyless approximation via the
o200k_base tokenizer — a non-Claude proxy, loudly labelled `≈`, never presented as ground
truth (the mode travels in the report and in `--json`). **API → exact, subscription →
approximate.**

## Build & test
```
dotnet build
dotnet test
```

## Layout
- `src/ContextTax.Core` — measurement engine (library)
- `src/ContextTax.Cli` — report-card CLI (`measure`)
- `src/ContextTax.Web` — dashboard (later)
- `tests/` — tests

## License
MIT — see [LICENSE](LICENSE).
