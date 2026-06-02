# Always Load

## When To Load

Load this index for every AI coding task, including implementation, refactoring, debugging, test writing, review, documentation updates, build changes, and release-related edits.

## Required Formal Standards

- `docs/engineering-standards/README.md`
- `docs/engineering-standards/01-foundation/engineering-principles.md`
- `docs/engineering-standards/01-foundation/terminology-and-exceptions.md`
- `docs/engineering-standards/02-collaboration/code-review.md`
- `docs/engineering-standards/03-project-and-code/project-structure.md`
- `docs/engineering-standards/03-project-and-code/coding-standards.md`
- `docs/engineering-standards/04-quality/testing-standards.md`
- `docs/engineering-standards/04-quality/security-standards.md`

## Execution Requirements

- Treat `docs/engineering-standards` as the only source of engineering rules.
- Do not infer rules from this index when a formal standard file is available.
- Combine this baseline with one language index and any matching task indexes.
- If a task conflicts with a formal standard, follow the formal standard or record an exception according to the formal exception process.
- When `.tw-memory/routes/standards.generated.yaml` exists and matches `source-index.generated.json`, use it only to locate the relevant formal-standard sections; do not treat generated memory cards as engineering rules.
- Do not load both a generated memory summary and the same formal standard text for the same rule. Prefer the formal standard text for decisions.
