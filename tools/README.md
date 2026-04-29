# 工具目录

本目录用于存放项目可复用的脚本、辅助工具和自动化处理资源。

## TW Memory CLI

`tools/tw-memory` contains the project-local AI memory engine. Use the command through the repository root:

```powershell
python tools\tw-memory\tw_memory.py <command>
```

The CLI owns scanning, generating, checking, querying, reading, preflight, postflight, and local search cache commands for `.tw-memory`.
