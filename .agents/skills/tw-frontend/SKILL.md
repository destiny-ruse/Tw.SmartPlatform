---
name: tw-frontend
description: Use for tasks that inspect or edit `frontend/**`.
---

# TW Frontend Development

Use this skill for frontend tasks under `frontend`.

## Preflight

Before editing, summarize the user's task into a short semantic query and run:

```powershell
python tools\tw-memory\tw_memory.py preflight --task "<semantic query>" --stack frontend --path "<target path>" --format brief
```

Read only the candidates returned by `preflight`, then call `query` and `read` for specific evidence chunks:

```powershell
python tools\tw-memory\tw_memory.py query --text "<specific query>" --stack frontend --format brief --limit 5
python tools\tw-memory\tw_memory.py read --chunk-id "<chunk id>" --format evidence
```

## Development Rules

Prefer internal frontend packages, app README files, and company standards before direct third-party usage. Do not open generated route shards for full parsing. Do not copy manuals or source bodies into `.tw-memory`.

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
