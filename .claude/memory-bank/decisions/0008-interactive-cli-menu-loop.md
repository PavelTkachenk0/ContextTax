# 0008 — Interactive CLI menu-loop (Spectre) over a full TUI; shared MeasurementRunner

- **Status:** Accepted (2026-06-07)

## Context
The CLI was purely flag-driven (`measure`/`session`/`servers <flags>`) — great for scripts/CI but
to use it by hand you had to remember flags, server names, and paths. We wanted a guided,
interactive mode that feels like a small console app. Separately, `measure` and `session` each
duplicated the resolve→count→measure sequence and its error-to-exit-code mapping (flagged in the
SP5 review).

## Decision
- **Interactive menu-loop built on Spectre.Console** (already a dependency): `contexttax` with no
  args launches a banner + menu (Measure / Session / List servers / Quit) with guided
  `SelectionPrompt`/`TextPrompt` steps (source · server-from-config · mode); an explicit
  `interactive` command is also registered, and the flag commands are unchanged. Implemented as the
  Spectre **default command** (`SetDefaultCommand<InteractiveCommand>()`).
- **Not a full-screen TUI (`Terminal.Gui`).** Rejected: a heavy new dependency against the project's
  low-context-tax/lean thesis, more code/risk/maintenance, and it duplicates the planned **web
  dashboard's** role as the "pretty/shareable" surface. A full TUI remains a low-regret option for a
  later sub-project if still wanted after the web dashboard.
- **Extract a shared `MeasurementRunner`** (+ typed `RunResult<T>`) as the single
  resolve→count→measure core. Both the flag commands and the interactive mode call it (real
  de-duplication, not relocation). The counter is supplied by the caller (`CounterFactory` owns its
  `HttpClient` lifetime); the runner maps every failure to a friendly `(message, exitCode)` so
  callers just render the result. (`RunResult`'s factories live on a non-generic class to satisfy
  CA1000.)
- **Counter checked before tool-source resolution** — a behavior-preserving reorder with an upside:
  with no API key (and not `--estimate`) the tool no longer needlessly connects to a live server
  before erroring. The only changed observable case is the rare double-error (no key **and** a bad
  source), which now surfaces the no-key error (exit 2); single-error and success paths are
  unchanged (verified by smoke + the runner tests).
- **Cancellable + never-crash:** every prompt returns to the menu (`← Back` on selections, a blank
  entry on text inputs); each menu action is wrapped so a single failure (e.g. a malformed MCP
  config — `McpConfig.List()` can throw) is shown and returns to the menu instead of tearing down
  the loop.
- **Secret-safe:** header values are entered masked (`.Secret()`) and never echoed; all user/error
  strings are `Markup.Escape`d before markup rendering.
- **No new dependency.**

## Consequences
- Effortless by-hand use; the flag commands stay byte-for-byte the same for scripts/CI.
- One measurement code path (the runner) — less drift; the engine and renderers are reused as-is.
- The interactive prompt glue is thin and **manually** smoke-tested; the real logic (the runner)
  is unit-tested headlessly via injected `IToolSource`/`ITokenCounter` fakes.
- Spectre `TextPrompt` has limited in-line cursor editing (←/→) — accepted; the back-navigation
  removes the only real trap. Rich, shareable presentation is deferred to the web dashboard.
- The counter-before-resolve reorder is the single documented behavioral nuance.
