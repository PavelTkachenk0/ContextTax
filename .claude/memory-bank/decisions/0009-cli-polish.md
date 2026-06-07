# 0009 — CLI polish: invariant culture, severity colour, ASCII bars, aliases, help

- **Status:** Accepted (2026-06-07)

## Context
Running the CLI live exposed rough edges: numbers rendered in the **machine's locale** (a Russian-locale
machine showed `0,6 %` / `1 145 tok` — wrong for a CLI), the `Context tax` figure was always
monochrome (no cheap/expensive signal), "Top offenders" was a plain token table, the long-only flags
(`--server`, `--estimate`, …) were verbose, and flag help didn't say what each option expects. The
maintainer's framing: **one great CLI beats two half-finished clients** — the web dashboard (SP7) was
frozen in its branch to focus here.

## Decision
A targeted polish of `ContextTax.Cli` only — **Core untouched, `--json` byte-identical**:
- **Locale-independent numbers.** `CultureInfo.DefaultThreadCurrentCulture/UICulture = Invariant` is set
  globally in `Program` **and** every renderer formats with explicit `InvariantCulture`. The global set
  fixes the whole process; the explicit per-format keeps a renderer correct (and unit-testable) even
  when called from a test under another culture. Defense in depth.
- **Severity colour.** New `Rendering/TaxSeverity` (percent → `Low<5 / Medium<10 / High` → Spectre
  `green`/`yellow`/`red`) colours the `Context tax` (measure) and peak `% window` (session) figures.
  Same thresholds the frozen web used.
- **Mini-bars.** New `Rendering/TokenBar` renders fixed-width `█/░` bars; "Top offenders" gains a
  severity-coloured bar column so offenders read at a glance.
- **Short aliases.** Every flag gets a one-letter alias via Spectre's `"-s|--server"` form
  (`-t/-s/-u/-H/-c/-m/-w/-j/-p/-e`; session `-f` = `--transcript`). `--header` is `-H` (capital) to
  avoid clashes; `--timeout` keeps no short.
- **Better help.** Each `[Description]` states what the flag expects with an example; `config.AddExample(...)`
  puts runnable invocations in `--help`.
- **Targeted, not a redesign.** Variant B (rework the card layout) was rejected as over-scoped; the
  cards stay — just correct, coloured, legible.
- **Test approach.** `TaxSeverity`/`TokenBar` are unit-tested; renderer output (invariance, bars,
  colour) is asserted with a `TestConsole` from a new **test-only** `Spectre.Console.Testing` dep;
  flag aliases are manual-smoke (Spectre parsing invokes the command body — awkward to unit-test).

## Consequences
- The CLI reads correctly on any locale, signals cheap/expensive at a glance, and is shorter to type.
- One new test-only package; no runtime deps; Core and the `--json` contract are unchanged.
- Severity thresholds now live in two places (CLI `TaxSeverity` + the frozen web's scale) — a future
  consolidation if/when the web unfreezes.
- Packaging (removing the `dotnet run` prefix) and the **automated response-measurement** feature are
  the next steps; packaging is deliberately deferred behind the response feature.
