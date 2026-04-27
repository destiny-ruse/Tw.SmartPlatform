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

## 新栈接入协议

taxonomy.yaml 的变更必须先于新栈代码提交。若代码先落地，`check-drift` 在首次 PR 时没有路径规则可匹配，首次提交会漏检。

标准步骤：

1. 在第一行代码之前约定目录结构，优先使用 `backend/{stack}/services/{name}/` 和 `backend/{stack}/packages/{name}/`。
2. 更新 `docs/knowledge/taxonomy.yaml` 的 `valid_stacks` 和 `path_rules`。
3. 运行全量扫描和图谱校验：

```powershell
python tools/knowledge/knowledge.py scan
python tools/knowledge/knowledge.py check
```

4. 单独提交 taxonomy 变更。
5. 新栈代码落地后运行：

```powershell
python tools/knowledge/knowledge.py scan
python tools/knowledge/knowledge.py init --kind module --id backend.go.services.example
python tools/knowledge/knowledge.py generate
python tools/knowledge/knowledge.py check
```

6. 后续 PR 使用增量兜底：

```powershell
python tools/knowledge/knowledge.py check-drift --from main --to HEAD
```

## 变更概览与历史记录

`diff` 只描述路径变更，不做漂移判断：

```powershell
python tools/knowledge/knowledge.py diff --from main --to HEAD
```

`check-drift --save` 会把诊断写入 `docs/knowledge/changes/<year>/`，文件格式与 `diagnostics.generated.json` 一致，并增加 `from_ref` 和 `to_ref`：

```powershell
python tools/knowledge/knowledge.py check-drift --from main --to HEAD --save
```

`docs/knowledge/proposals/` 只存放候选图谱更新，正式图谱仍需人工审核后写入 `docs/knowledge/graph/`。
