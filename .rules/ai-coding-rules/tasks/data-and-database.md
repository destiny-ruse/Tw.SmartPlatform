# Data And Database

## When To Load

Load this index when a task touches database schema, table naming, field naming, index, constraint, audit fields, tenant boundary, soft delete, migration, data backfill, data repair, import, export, or cross-system data format.

## Required Formal Standards

- `docs/engineering-standards/03-project-and-code/data-and-database.md`
- `docs/engineering-standards/04-quality/testing-standards.md`
- `docs/engineering-standards/04-quality/security-standards.md`

## Execution Requirements

- Read the formal data and database standard before changing schema, migration files, data access models, data repair scripts, or data formats.
- Identify forward compatibility, rollback strategy, data preservation, tenant isolation, audit fields, and production verification.
- For release sequencing or rollback tasks, also load `tasks/ci-cd-and-release.md`.

