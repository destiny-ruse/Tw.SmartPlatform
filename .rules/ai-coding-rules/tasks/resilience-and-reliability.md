# Resilience And Reliability

## When To Load

Load this index when a task touches idempotency, retry, timeout, rate limit, circuit breaker, degradation, cache, message consumption, dead letter handling, compensation, SLO, health check, or failure recovery.

## Required Formal Standards

- `docs/engineering-standards/04-quality/resilience-and-reliability.md`
- `docs/engineering-standards/05-delivery-and-operations/observability-and-operations.md`

## Execution Requirements

- Read the formal resilience and reliability standard before changing failure handling or external dependency behavior.
- Define timeout, retry conditions, idempotency, fallback behavior, observability, and recovery path.
- For message contracts or API idempotency, also load `tasks/api-design.md`.

