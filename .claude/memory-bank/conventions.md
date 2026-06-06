# Conventions

## Code style
- `.editorconfig` is authoritative; `dotnet format --verify-no-changes` must pass.
- `Nullable` enabled; compiler warnings are errors. No suppressions without a comment
  saying why.
- A vulnerable transitive package breaks the warnings-as-errors build (NU1903). Pin it to
  a patched version via CPM transitive pinning (`CentralPackageTransitivePinningEnabled` +
  a `PackageVersion`), with a comment citing the advisory — don't suppress the warning.

## Testing
- TDD per `superpowers:test-driven-development`: write the failing test first.
- xUnit. Tests live in `tests/<Project>.Tests`.
- Test method names use underscores (e.g. `Name_is_ContextTax`); test projects
  suppress CA1707 with a justifying comment (warnings-as-errors would otherwise fail).
- **Gated integration tests** (those that touch the network or spawn a process) are
  **opt-in via an environment variable** and skip by early `return` when it is unset, so CI
  stays hermetic — no network calls, no external packages pulled. `ANTHROPIC_API_KEY` gates
  the live `count_tokens` test; `CONTEXTTAX_LIVE_TESTS=1` gates the live MCP ingestion test.
  (xUnit v2 has no conditional `Skip`; early-return reports as *passed* — a true skip would
  need the `Xunit.SkippableFact` dependency, deliberately avoided.)

## Mapping & serialization
- Map between models in a dedicated static class named `{Target}Mapper` with a single
  `Map(...)` method — e.g. `CountTokensRequestMapper.Map(model, tools)`. One mapper per
  target type.
- DTOs / wire records stay **pure data**: no `For`/`From` factories and no domain-model
  references on them. The mapper owns the domain↔wire translation.
- Serialize wire models as **typed records + a shared `JsonSerializerOptions`** (e.g.
  `CountTokensJson.Options`, snake_case + ignore-null) — not a hand-built
  `JsonNode` / `JsonObject` DOM. Let the naming policy do renames like `inputSchema` →
  `input_schema`.
- Keep separation of responsibilities clean across all classes (transport / serialization
  / mapping / domain semantics live in distinct units).

## Commits & branches
- Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:`, `ci:`, `test:`, `build:`).
- Each commit message ends with the project's `Co-Authored-By` trailer.
- Work on feature branches off `main`; integrate via PR
  (`superpowers:finishing-a-development-branch`).

## Public-repo hygiene (hard rules)
- Never commit secrets, API keys, or personal absolute paths.
- Anthropic key: `ANTHROPIC_API_KEY` (env) or `dotnet user-secrets` only.
- `docs/superpowers/` is gitignored — internal process journal, local-only.
- CI enforces a secret scan (gitleaks) + a personal-path scan. The optional local
  `.githooks/pre-commit` mirrors these — enable with
  `git config core.hooksPath .githooks`.

## Language
English for all repo artifacts; Russian only in live chat.

## The workflow loop (Definition of Done)
Every superpowers cycle ends with **`/sync-memory`**: read the latest plan in
`docs/superpowers/plans/`, then update the memory bank — `roadmap.md` always;
`architecture.md` when structure changed; a new ADR via `/adr` for notable decisions;
`conventions.md` if a new pattern was set.
