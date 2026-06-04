# Roadmap

## Current status
**Sub-project 1 — Repository Foundation: ✅ complete.**
Shipped: a compiling .NET 10 walking skeleton (Core + CLI + Web + Core.Tests, CPM,
warnings-as-errors), the AI dev infrastructure (lean CLAUDE.md, memory bank, `/adr`
and `/sync-memory`), public-repo hygiene, and CI. Build / test / format green. No
product features yet — that begins in sub-project 2.

## Decomposition
1. **Repository Foundation** — ✅ done.
2. **Core measurement engine** — static schema-token cost via `count_tokens` (ground
   truth). The first real product capability.
3. **CLI report card** — render a server's token report.
4. **Response-bloat + full-lifecycle** measurement (the wedge).
5. **Strategy comparison harness** — static / tool-search / dynamic / progressive /
   code mode; reproducible, with variance.
6. **Web dashboard / leaderboard.**

## Deferred / open
- **Claude Code hooks** — revisit (maintainer flagged). Candidate: a commit-guard hook
  complementing CI.
- **Web tech** — Blazor vs static-HTML generator vs API+SPA — decide in sub-project 6.
- **Cross-model support** — per-provider token counting beyond Anthropic.
- **CI vs preview SDK** — `global.json` pins a .NET 10 preview build that GitHub's
  `setup-dotnet` may not fetch; switch to the GA pin (preferred) or CI
  `dotnet-quality: preview` before relying on first-push CI being green.

## Next
Start sub-project 2 (Core measurement engine) with its own spec → plan → implement →
`/sync-memory` cycle.
