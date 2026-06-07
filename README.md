<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="assets/hero-dark.svg">
    <img src="assets/hero-light.svg" alt="ContextTax — the GitHub MCP server taxes your agent ~8,616 tokens before it reads a word" width="100%">
  </picture>
</p>

<p align="center">
  <a href="https://github.com/PavelTkachenk0/ContextTax/releases/latest"><img src="https://img.shields.io/github/v/release/PavelTkachenk0/ContextTax?style=flat-square&color=CC785C" alt="release"></a>
  <a href="https://github.com/PavelTkachenk0/ContextTax/releases"><img src="https://img.shields.io/github/downloads/PavelTkachenk0/ContextTax/total?style=flat-square" alt="downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue?style=flat-square" alt="MIT"></a>
  <a href="https://github.com/PavelTkachenk0/ContextTax/actions/workflows/ci.yml"><img src="https://img.shields.io/github/actions/workflow/status/PavelTkachenk0/ContextTax/ci.yml?style=flat-square&label=CI" alt="CI"></a>
  <img src="https://img.shields.io/badge/macOS%20%C2%B7%20Linux%20%C2%B7%20Windows-555?style=flat-square" alt="platforms">
</p>

<p align="center">
  <picture><img src="assets/demo.svg" alt="contexttax measure --server github — ~8,616 tokens, 4.3% of a 200,000 window" width="82%"></picture>
</p>

<p align="center">
  <a href="https://github.com/PavelTkachenk0/ContextTax/releases/latest"><img src="https://img.shields.io/badge/%E2%AC%87_Download_latest-CC785C?style=for-the-badge" alt="Download latest"></a>
</p>

---

## Install

**macOS / Linux**
```sh
curl -fsSL https://github.com/PavelTkachenk0/ContextTax/releases/latest/download/install.sh | sh
```

**Windows (PowerShell)**
```powershell
irm https://github.com/PavelTkachenk0/ContextTax/releases/latest/download/install.ps1 | iex
```

Or download a single-file binary from [Releases](https://github.com/PavelTkachenk0/ContextTax/releases/latest) — **no .NET required**.

> The binaries are unsigned. **macOS:** `xattr -d com.apple.quarantine ./contexttax` (or right-click → Open). **Windows:** "More info → Run anyway".

## Usage

```sh
# Measure a server's schema cost (a live MCP server from your config):
contexttax measure --server github

# Measure a captured tool response — paste it straight in:
pbpaste | contexttax response -e

# Compare a response before/after an optimisation:
contexttax response before.json -d after.json -e

# Measure a whole recorded session (schema + every response):
contexttax session -f run.json -t tools.json -e

# List the MCP servers discovered in your config:
contexttax servers

# No arguments → interactive menu:
contexttax
```

Every command takes **`-e/--estimate`** (keyless, offline o200k_base proxy) or, with `ANTHROPIC_API_KEY` on a funded account, ground-truth `count_tokens`. Add **`--json`** for machine-readable output, **`--window N`** to set the context window, **`-h`** for help.

## Downloads

| Platform | Architecture | File |
|----------|--------------|------|
| macOS | Apple Silicon | `contexttax-osx-arm64` |
| macOS | Intel | `contexttax-osx-x64` |
| Linux | x64 | `contexttax-linux-x64` |
| Windows | x64 | `contexttax-win-x64.exe` |

All binaries are self-contained and single-file — they bundle the .NET runtime, so nothing else is needed.

## Why

A stack of MCP servers can burn **50–200K tokens** of an agent's context window *before the user says a word* — schema bloat up front, then every tool response piling on. ContextTax measures that cost per server in two currencies: **tokens / % of window** and **$ (API)**. The headline metric: *% of context window spent before useful work begins.*

**Two accuracy modes, never mixed up.** With a funded `ANTHROPIC_API_KEY` you get exact `count_tokens` numbers (`✓ ground truth`). Without one, **`-e/--estimate`** gives a keyless o200k_base approximation, loudly labelled `≈` — never presented as ground truth.

## The numbers, reproduced

ContextTax measures itself against real MCP servers. Schema cost is **deterministic** — run the same command against the same server version and you get the same number. That's the point: reproducible, so you can cite it.

| MCP server | Tools | Schema tokens | % of a 200K window |
|------------|------:|--------------:|-------------------:|
| Playwright | 23 | ~3,239 | 1.6% |
| **GitHub** (default toolset) | 43 | **~8,616** | **4.3%** |
| GitHub (all toolsets) | 82 | ~16,327 | 8.2% |
| Azure | 65 | ~16,364 | 8.2% |

<sub>≈ measured with ContextTax's offline **o200k_base estimate** (not Anthropic `count_tokens`), 200K window. GitHub measured against `ghcr.io/github/github-mcp-server` (default toolset) — your numbers will match for the same server version.</sub>

Reproduce the headline:
```sh
contexttax measure --server github -e --window 200000
```

## Build from source

```sh
dotnet build
dotnet run --project src/ContextTax.Cli -- measure --tools samples/tools/filesystem.tools.json -e
```

## License

MIT — see [LICENSE](LICENSE).
