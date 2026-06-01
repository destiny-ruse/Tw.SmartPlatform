# Runtime And Infrastructure

## When To Load

Load this index when a task touches runtime environment, environment variables, configuration files, configuration center, secrets, container image, image tag, Kubernetes Deployment, Service, Ingress, ConfigMap, Secret, Job, CronJob, or resource labels.

## Required Formal Standards

- `docs/engineering-standards/05-delivery-and-operations/runtime-and-infrastructure.md`
- `docs/engineering-standards/04-quality/security-standards.md`
- `docs/engineering-standards/04-quality/dependency-and-build.md`

## Execution Requirements

- Read the formal runtime and infrastructure standard before changing environment, configuration, container, or Kubernetes resources.
- Keep configuration and secrets separated, preserve artifact traceability, and avoid source changes for environment switching.
- For deployment or rollback behavior, also load `tasks/ci-cd-and-release.md`.

