---
name: tw-memory
description: Use only when maintaining the TW memory system itself: `.tw-memory`, `tools/tw-memory`, memory indexes, generated chunk metadata, or TW memory thin skills.
---

# TW Memory System Maintenance

Use only when maintaining the TW memory system itself. Do not use this skill for ordinary product code.

## Required Flow

1. Run focused tests for the memory engine before changing behavior.
2. Change `tools/tw-memory` or `.tw-memory` contracts through the CLI.
3. Run generation and checks after contract changes:

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
python -m unittest discover tests\tw_memory -v
```

## Boundaries

`.tw-memory` stores indexes, graph metadata, route indexes, chunk metadata, summaries, keywords, relations, hashes, and evidence pointers. It must not store manual bodies, standards bodies, source copies, third-party docs, source archives, chat logs, secrets, SQLite files, vector caches, or web captures.
