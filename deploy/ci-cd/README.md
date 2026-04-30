# 持续交付目录

本目录用于存放持续集成、持续交付、流水线配置和自动化发布相关资源。

## TW Memory Check

CI should run this command from the repository root:

```powershell
.\deploy\ci-cd\tw-memory-check.ps1
```

The script validates `.tw-memory` freshness, source hashes, route paths, chunk line ranges, forbidden runtime cache files, and repository whitespace issues. It does not generate files in CI.
Set `TW_MEMORY_DIFF_BASE` (for example, `origin/master`) in CI to check committed branch whitespace with `git diff --check <base>...HEAD`; without it, the script checks the working tree diff.
