---
name: tw-skill-linker
description: Use when adding, renaming, or syncing Tw.SmartPlatform repository Skills for Claude Code relative symlink discovery.
---

# Skill Linker

Use this to synchronize Claude Code Skill adapters.

## Command

Run:

```powershell
python tools/knowledge/knowledge.py sync-skills --target claude
```

## Rules

`.agents/skills` is the only Skill source. `.claude/skills` must contain relative symlinks pointing to `.agents/skills`; do not copy Skill bodies into `.claude/skills`.
