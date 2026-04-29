---
name: tw-python
description: Use for tasks that inspect or edit `backend/python/**`.
---

# TW Python Development

Use this skill for Python tasks under `backend/python`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack python --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack python --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal Python service README files, scripts documented in the repository, and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

## Postflight

After edits, classify memory impact:

```powershell
python tools\tw-memory\tw_memory.py postflight --changed-files "<changed files>" --format brief
```

If postflight requests regeneration, run:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```
