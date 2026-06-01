# API Design

## When To Load

Load this index when a task touches REST, HTTP JSON API, gRPC, AsyncAPI, OpenAPI, response structure, error response, status code, input validation, idempotency, SDK generation, interface Mock, or cross-team API contracts.

## Required Formal Standards

- `docs/engineering-standards/03-project-and-code/api-design.md`
- `docs/engineering-standards/03-project-and-code/coding-standards.md`
- `docs/engineering-standards/04-quality/testing-standards.md`

## Execution Requirements

- Read the formal API standard before changing API routes, request models, response models, error codes, status codes, message contracts, or generated clients.
- When API behavior changes, check contract tests, Mock data, SDK impact, compatibility, and migration requirements.
- For authentication, authorization, sensitive data, or input boundary changes, also load `tasks/security.md`.

