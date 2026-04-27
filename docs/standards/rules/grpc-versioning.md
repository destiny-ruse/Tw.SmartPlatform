---
id: rules.grpc-versioning
title: gRPC 版本演进规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [grpc, compatibility, versioning]
summary: 规定gRPC 服务版本、字段兼容和废弃流程的设计和验证要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# gRPC 版本演进规范

<!-- anchor: goal -->
## 目标

gRPC 版本演进规范用于降低字段号复用、包版本混乱和服务升级破坏旧客户端的风险。gRPC 契约必须用可审查的兼容性规则演进，使多语言客户端可以按版本稳定生成和迁移。版本策略也必须和契约测试、变更治理保持一致。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `proto/`、`apis/`、`services/`、`src/` 中 gRPC 包版本、服务版本、字段新增和删除、枚举演进、废弃标记、生成客户端和契约测试。它不适用于 REST 路由版本、AsyncAPI 通道版本或数据库迁移版本；这些版本只有在映射到 gRPC 契约时需要参考本规范。

<!-- anchor: rules -->
## 规则

1. 对外 gRPC 包名必须包含版本段，例如 `orders.v1`；同一破坏性版本内不得改变既有字段含义。
2. 新增字段必须使用新的字段号，并保证旧客户端忽略后仍能工作；不得把新字段作为旧方法成功调用的隐式必需条件。
3. 删除字段必须先标记 `deprecated = true`，随后在破坏性版本中移除并使用 `reserved` 保留字段号和字段名；不得复用已发布字段号。
4. 枚举新增值必须考虑旧客户端未知枚举处理；不得改变已有枚举数值或语义。
5. 改变字段类型、字段号、包名、服务名、方法签名、流式类型或错误语义必须视为破坏性变更。
6. 破坏性变更必须创建新包版本或新服务版本，并保留旧版本迁移说明和契约测试。
7. 废弃字段、方法或服务必须在 proto 注释和变更说明中标注替代方案；不得只依赖口头约定。
8. 版本演进必须纳入变更治理，涉及多个消费者时应当在合并前确认契约测试覆盖。

<!-- anchor: examples -->
## 示例

正例：

```proto
message Order {
  string id = 1;
  reserved 2;
  reserved "legacy_status";
  OrderStatus status = 3;
}
```

反例：

```proto
message Order {
  string id = 1;
  int64 paid_at = 2; // 原字段 2 曾经是 legacy_status
}
```

<!-- anchor: checklist -->
## 检查清单

- 包名、服务和生成配置是否包含并使用一致版本。
- 字段新增是否向后兼容，且旧客户端忽略后仍能工作。
- 删除字段是否先废弃，并在移除后使用 `reserved`。
- 枚举新增和错误语义变化是否评估旧客户端行为。
- 破坏性变更是否创建新版本并提供迁移说明。

<!-- anchor: relations -->
## 相关规范

- rules.grpc-proto-style
- rules.contract-testing
- processes.change-governance

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
