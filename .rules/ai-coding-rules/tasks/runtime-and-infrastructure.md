# Runtime And Infrastructure

## When To Load

Load this index when a task touches runtime environment, environment variables, configuration files, configuration center, secrets, container image, image tag, Kubernetes Deployment, Service, Ingress, ConfigMap, Secret, Job, CronJob, or resource labels.

## Required Formal Standards

- `docs/engineering-standards/05-delivery-and-operations/runtime-and-infrastructure.md`
- `docs/engineering-standards/04-quality/security-standards.md`
- `docs/engineering-standards/04-quality/dependency-and-build.md`

## Execution Requirements

- Read the referenced formal standard files before changing this task area.
- If this task overlaps another task category, load the matching task index from `01-task-router.md`.
- Use `.tw-memory/routes/standards.generated.yaml` only to locate formal-standard sections when the generated memory index is current.
