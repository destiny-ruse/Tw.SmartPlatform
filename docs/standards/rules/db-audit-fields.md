---
id: rules.db-audit-fields
title: 数据库审计字段规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, qa, ai]
stacks: [dotnet, java, python]
tags: [data, storage]
summary: 规定创建、更新、删除和租户审计字段。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 数据库审计字段规范

<!-- anchor: goal -->
## 目标

数据库审计字段规范用于降低数据来源不可追踪、租户边界缺失和软删除语义不一致的风险。它要求常用审计字段在名称、类型、写入时机和查询边界上保持一致，使数据修复、问题排查和合规审查可以复核。

<!-- anchor: scope -->
## 适用范围

适用于业务数据库表、数据访问层实体、ORM 映射、迁移脚本、数据修复脚本和涉及创建、更新、删除、租户隔离的查询。日志表、事件溯源存储、临时导入表和外部只读同步表可以按自身模型裁剪，但必须在设计或评审中说明不适用字段和原因。

<!-- anchor: rules -->
## 规则

1. 业务表必须包含 `created_at` 和 `updated_at`，语义分别为记录创建时间和最后业务变更时间，不得复用为导入时间或同步时间。
2. 需要记录操作者的业务表应当使用 `created_by` 和 `updated_by`，值必须来自已认证主体、系统任务标识或明确的迁移操作者标识。
3. 多租户表必须包含 `tenant_id` 或等价租户边界字段，所有常规查询必须带租户条件；全局共享表必须在设计中标明共享原因。
4. 采用软删除的表必须使用一致的删除字段，例如 `deleted_at` 和可选 `deleted_by`，不得同时混用 `is_deleted`、`delete_flag` 等多套语义。
5. 软删除数据默认不得出现在业务读取、唯一性校验和列表查询中；需要读取已删除数据时必须在代码或查询中显式表达。
6. 审计时间字段必须使用统一时区语义和可排序类型，不得使用本地化字符串保存时间。
7. 数据修复、回填和迁移脚本必须维护审计字段语义，不得把批量执行时间覆盖为原始创建时间，除非变更说明中明确记录。
8. 审计字段不得存储个人敏感信息明文；需要人员信息时应当保存稳定主体 ID。

<!-- anchor: examples -->
## 示例

正例：

```sql
create table order_item (
  id bigint primary key,
  tenant_id bigint not null,
  order_id bigint not null,
  sku_id bigint not null,
  created_at timestamp not null,
  created_by varchar(64) not null,
  updated_at timestamp not null,
  updated_by varchar(64) not null,
  deleted_at timestamp null,
  deleted_by varchar(64) null
);
```

正例说明：字段名称和语义一致，包含租户、创建、更新和软删除边界。

反例：

```sql
select * from order_item where order_id = 42;
```

反例说明：多租户表查询缺少 `tenant_id`，也未排除软删除记录。

<!-- anchor: checklist -->
## 检查清单

- 表是否按数据生命周期声明创建、更新、删除和租户字段。
- 审计字段名称、类型和写入语义是否与本规范一致。
- 多租户查询是否默认包含租户边界。
- 软删除表的业务查询是否默认排除已删除记录。
- 数据修复或回填是否保留原始审计语义并记录操作者。
- 审计字段是否避免存储个人敏感信息明文。

<!-- anchor: relations -->
## 相关规范

- rules.db-naming
- rules.pii-handling
- rules.tracing

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
