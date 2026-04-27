---
id: rules.db-naming
title: 数据库命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, qa, ai]
stacks: [dotnet, java, python]
tags: [data, storage]
summary: 规定库、表、列、索引和约束命名。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 数据库命名规范

<!-- anchor: goal -->
## 目标

数据库命名规范用于降低表意不清、跨团队命名冲突和迁移审查困难的风险。它要求 schema、表、列、索引和约束名称具备稳定、可读、可搜索的统一形式，使数据模型能够被代码、迁移和运维人员一致理解。

<!-- anchor: scope -->
## 适用范围

适用于数据库 schema、表、视图、列、索引、主键、外键、唯一约束、检查约束、迁移脚本和 ORM 映射中的数据库对象名称。第三方产品自带表、外部只读同步库和迁移工具生成的内部元数据表可以保留原名，但本仓库新增对象必须遵守本规范。

<!-- anchor: rules -->
## 规则

1. 数据库对象名称必须使用小写 `snake_case`，不得混用大小写、空格、拼音缩写或不稳定业务代号。
2. schema 名称应当表达业务域或边界上下文；不得使用 `common`、`misc`、`temp` 等无法说明所有权的名称承载长期对象。
3. 表名必须使用单数或复数中的一种稳定约定，并在同一 schema 内保持一致；关联表名称应当表达两端实体和关系语义。
4. 列名必须表达业务含义和单位，时间字段、金额字段、状态字段不得使用 `time`、`amount`、`flag` 等缺少上下文的泛名。
5. 主键列可以使用 `id`，外键列必须使用 `<referenced_entity>_id`，多租户字段必须使用 `tenant_id` 或在设计中说明等价边界。
6. 索引名称必须体现表名、列名和索引目的，推荐 `idx_<table>_<columns>`；唯一索引推荐 `uk_<table>_<columns>`。
7. 约束名称必须体现约束类型和作用对象，推荐 `pk_<table>`、`fk_<table>_<column>`、`ck_<table>_<rule>`。
8. 重命名数据库对象必须通过迁移说明兼容影响，不得只为风格统一在高风险发布中批量重命名稳定对象。

<!-- anchor: examples -->
## 示例

正例：

```sql
create table sales_order (
  id bigint primary key,
  tenant_id bigint not null,
  customer_id bigint not null,
  total_amount_cents bigint not null,
  created_at timestamp not null,
  constraint fk_sales_order_customer_id foreign key (customer_id) references customer(id)
);

create index idx_sales_order_tenant_id_created_at on sales_order (tenant_id, created_at);
```

正例说明：表、列、约束和索引都能从名称看出对象和用途。

反例：

```sql
create table T_Order (
  UID bigint,
  amt decimal(18, 2),
  flag int
);
```

反例说明：大小写、缩写和泛化列名都降低可读性和可审查性。

<!-- anchor: checklist -->
## 检查清单

- 数据库对象是否统一使用小写 `snake_case`。
- schema 和表名是否表达业务域、所有权和实体语义。
- 列名是否包含必要上下文、单位和关系边界。
- 主键、外键、租户字段和审计字段命名是否一致。
- 索引和约束名称是否体现类型、表名和关键列。
- 重命名或清理命名债务是否有迁移兼容和回滚说明。

<!-- anchor: relations -->
## 相关规范

- rules.naming-common
- rules.db-audit-fields
- rules.db-migration

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
