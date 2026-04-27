---
name: tw-requirement-router
description: Use before implementing or modifying behavior in Tw.SmartPlatform to route business requirements to existing capabilities, contracts, standards, and service integration checks.
---

# Requirement Router

Use this before code changes for features, service behavior changes, frontend behavior changes, contract changes, or tool changes.

## Flow

1. Read `docs/knowledge/generated/index.generated.json`.
2. Read `docs/knowledge/taxonomy.yaml` for `query_aliases`.
3. Extract business objects, actions, risks, and target delivery area from the user request.
4. Run or simulate `python tools/knowledge/knowledge.py query --text "<request>" --limit 5`.
5. If the request may use an existing capability, invoke `tw-knowledge-discovery`.
6. If the request may involve one service calling another service, invoke `tw-service-integration`.
7. If generated diagnostics show drift, invoke `tw-knowledge-maintenance`.

## Output

Respond in Chinese with:

- 识别到的能力域
- 需要读取的知识图谱节点
- 需要进入的后续 Skill
- 当前不能直接写代码的阻塞项
