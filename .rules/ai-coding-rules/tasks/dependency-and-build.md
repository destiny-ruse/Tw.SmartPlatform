# Dependency And Build

## When To Load

Load this index when a task touches package manager files, dependency versions, lock files, build scripts, compilation, packaging, artifacts, container image build, dependency scanning, or open-source license checks.

## Required Formal Standards

- `docs/engineering-standards/04-quality/dependency-and-build.md`

## Execution Requirements

- Read the formal dependency and build standard before adding dependencies, changing lock files, changing build scripts, or changing artifact generation.
- Check dependency purpose, maintenance, security history, license, transitive dependency impact, and rollback path.
- For container runtime or Kubernetes deployment changes, also load `tasks/runtime-and-infrastructure.md`.

