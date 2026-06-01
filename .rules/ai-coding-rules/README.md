# AI Coding Rules Index

## Purpose

This directory is the AI-facing index for the Chengdu Tianwen Internet engineering standards.

The source of truth is `docs/engineering-standards`. Files in this directory only tell AI coding tools which formal standards to load for a task. They do not replace, summarize, or fork the formal standards.

## How To Use

1. Load `00-always-load.md` for every coding, refactoring, debugging, review, test, or documentation task.
2. Load `01-task-router.md` to select language-specific and task-specific indexes.
3. Load only the matched files under `languages/` and `tasks/`.
4. Read the formal standard files referenced by the matched indexes before making code changes.

## Maintenance Rule

When the content of an existing formal standard changes, this index should not need a matching edit. Update this index only when formal standard file paths, supported languages, or task categories change.

## Directory Layout

```text
.rules/ai-coding-rules/
|-- README.md
|-- 00-always-load.md
|-- 01-task-router.md
|-- languages/
`-- tasks/
```

