# Task Router

## Language Indexes

Load the matching language index when the task touches source code in that technology:

- `.NET Core`: `languages/dotnet-core.md`
- `Java`: `languages/java.md`
- `Python`: `languages/python.md`
- `Vue`: `languages/vue.md`
- `uni-app`: `languages/uni-app.md`
- `TypeScript`: `languages/typescript.md`
- `JavaScript`: `languages/javascript.md`

When multiple languages are touched, load each matching language index.

## Task Indexes

Load the matching task index when the task touches the listed area:

- API, REST, HTTP, gRPC, AsyncAPI, OpenAPI, response shape, error response, SDK, Mock: `tasks/api-design.md`
- Database schema, migration, audit fields, data backfill, data repair: `tasks/data-and-database.md`
- Test strategy, unit tests, integration tests, end-to-end tests, contract tests, fixtures, coverage: `tasks/testing.md`
- Dependency, package manager, lock file, build script, artifact, container image, license: `tasks/dependency-and-build.md`
- Authentication, authorization, OAuth, OIDC, secret, personal information, input validation: `tasks/security.md`
- Idempotency, cache, message, timeout, retry, rate limit, circuit breaker, degradation, SLO, health check: `tasks/resilience-and-reliability.md`
- CI/CD, pipeline, quality gate, release, rollback, environment promotion: `tasks/ci-cd-and-release.md`
- Logs, metrics, tracing, alerting, runbook, incident review: `tasks/observability-and-operations.md`
- Environment variable, configuration, runtime, container, Kubernetes, ConfigMap, Secret: `tasks/runtime-and-infrastructure.md`
- Code review, architecture review, quality metrics, governance, exception: `tasks/review-and-governance.md`

## Loading Order

1. `00-always-load.md`
2. Matching `languages/*.md`
3. Matching `tasks/*.md`
4. Formal standard files referenced by those indexes

