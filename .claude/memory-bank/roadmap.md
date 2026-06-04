# Roadmap

## Current status
**Sub-project 1 — Repository Foundation: in progress.**
AI dev infrastructure + a compiling .NET 10 walking skeleton (Core + CLI + Web),
public-repo hygiene, CI. No product features yet.

## Decomposition
1. **Repository Foundation** — this one.
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

## Next
Start sub-project 2 (Core measurement engine) with its own spec → plan → implement →
`/sync-memory` cycle.
