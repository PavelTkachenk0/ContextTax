# Conventions

## Code style
- `.editorconfig` is authoritative; `dotnet format --verify-no-changes` must pass.
- `Nullable` enabled; compiler warnings are errors. No suppressions without a comment
  saying why.

## Testing
- TDD per `superpowers:test-driven-development`: write the failing test first.
- xUnit. Tests live in `tests/<Project>.Tests`.
- Test method names use underscores (e.g. `Name_is_ContextTax`); test projects
  suppress CA1707 with a justifying comment (warnings-as-errors would otherwise fail).

## Mapping
- Map between models in a dedicated static class named `{Target}Mapper` with a single
  `Map(...)` method — e.g. `CountTokensRequestMapper.Map(model, tools)`. One mapper per
  target type.
- DTOs / wire records stay **pure data**: no `For`/`From` factories and no domain-model
  references on them. The mapper owns the domain↔wire translation.
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
