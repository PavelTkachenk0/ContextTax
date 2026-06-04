---
description: Scaffold a new Architecture Decision Record in the memory bank
---

Create a new ADR in `.claude/memory-bank/decisions/`.

1. List existing files in `.claude/memory-bank/decisions/` to find the highest `NNNN`
   number; the new number is that + 1, zero-padded to 4 digits.
2. Derive a short kebab-case slug from the decision title in `$ARGUMENTS` (ask if it
   is empty).
3. Create `NNNN-<slug>.md` from this template, filled in:

   ```
   # NNNN — <Title>

   - **Status:** Accepted (<today's date>)

   ## Context
   <why this decision is needed>

   ## Decision
   <what we decided>

   ## Consequences
   <trade-offs and follow-ups>
   ```
4. Keep it to one decision. Show the created file path.
