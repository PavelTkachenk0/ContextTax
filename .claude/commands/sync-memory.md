---
description: Reconcile the in-repo memory bank from the latest superpowers plan
---

Fold the outcomes of the most recent superpowers cycle back into the memory bank.
This is the Definition of Done for every cycle.

1. Find the most recent plan in `docs/superpowers/plans/` (and its spec in
   `docs/superpowers/specs/`). If `$ARGUMENTS` names a specific plan, use that.
2. Review what was actually built (git log / diff since the cycle started).
3. Propose **minimal** edits to `.claude/memory-bank/`:
   - `roadmap.md` — mark the sub-project/feature done; set "Next". **Always.**
   - `architecture.md` — if components / boundaries / dependencies changed.
   - `decisions/` — a new ADR (use the `/adr` template) for any notable decision not
     yet recorded.
   - `conventions.md` — only if a new convention/pattern was established.
4. Show the proposed diffs and apply after confirmation.
5. Do **not** copy raw plan prose into the memory bank — distill. Plans are the
   journal; the memory bank is the current truth.

Targets layer 2 (the in-repo project memory bank), not layer 1 (personal memory — that
is the `consolidate-memory` skill).
