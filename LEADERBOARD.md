# MCP Context-Tax Leaderboard

The context-window cost of an MCP server's **tool schemas** — what it spends *before your agent reads a word*. Measured with [ContextTax](README.md): Anthropic `count_tokens` (`claude-sonnet-4-5`, 200K window). **Leaner is better.**

| Rank | MCP server | Tools | Schema tokens (`✓ count_tokens`) | % of 200K window |
|-----:|------------|------:|---------------------------------:|-----------------:|
| 1 | Playwright | 23 | 4,633 | 2.3% |
| 2 | GitHub · default toolset | 43 | 10,928 | 5.5% |
| 3 | Azure | 65 | 18,983 | 9.5% |
| 4 | GitHub · all toolsets | 82 | 20,404 | 10.2% |

<sub>Ground-truth (`count_tokens`) figures. Estimate (o200k) numbers + methodology + the "why not 55K?" reconciliation are in the [README](README.md#the-numbers-reproduced). Measured against pinned server versions.</sub>

## Add your server

PRs welcome — this list is community-driven.

1. **Measure it** (ground truth needs a funded `ANTHROPIC_API_KEY`; `-e` is the keyless o200k estimate):
   ```sh
   contexttax measure --server <name> --window 200000
   ```
2. **Add a row** with: server name, tool count, schema tokens, % of a 200K window, and — in your PR description — the **exact command + the server version** you measured (so anyone can reproduce it).
3. Keep the table **sorted by tokens ascending** (leanest first). One server = one row; note the toolset config if it has one.

Ground-truth (`✓ count_tokens`) entries rank above keyless estimates. Optimised your server's schema? Re-measure and send a PR to climb. 📉
