# Security

## When To Load

Load this index when a task touches authentication, authorization, OAuth, OIDC, role checks, tenant checks, secret handling, personal information, input validation, file upload, callback verification, audit log, data export, or dependency vulnerability handling.

## Required Formal Standards

- `docs/engineering-standards/04-quality/security-standards.md`
- `docs/engineering-standards/03-project-and-code/coding-standards.md`

## Execution Requirements

- Read the formal security standard before changing security boundaries or handling sensitive data.
- Never rely on front-end checks as the only authorization or validation control.
- Check logs, errors, test data, examples, and configuration for secrets or personal information exposure.

