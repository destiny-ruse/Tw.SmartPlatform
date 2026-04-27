# 仓库知识图谱

本目录保存 Tw.SmartPlatform 的仓库级知识图谱，用于描述已有能力、模块、契约、集成关系和架构决策。

`graph/` 是正式事实源，可以由人工维护，也可以由开发者显式调用 AI Skill 后维护。`generated/` 是工具生成产物，不得手工编辑。`changes/` 保存 git diff 影响分析。`proposals/` 保存可选候选更新。

常用命令：

```powershell
python tools/knowledge/knowledge.py generate
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py check-drift --from main --to HEAD
python tools/knowledge/knowledge.py query --text "获取当前用户信息" --limit 5
python tools/knowledge/knowledge.py sync-skills --target claude
```

AI 工具必须先读取 `docs/knowledge/generated/index.generated.json`，再按需读取 L1 分片、L2 section index 和目标 YAML 字段行范围。
