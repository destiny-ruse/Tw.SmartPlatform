---
id: rules.test-strategy
title: 测试策略规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [testing, quality]
summary: 规定单元、集成、契约、端到端和人工验证边界。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 测试策略规范

<!-- anchor: goal -->
## 目标

测试策略规范用于降低团队把所有风险都压到单一测试层级、遗漏关键验证或依赖不可重复人工判断的风险。
每项变更必须选择能证明行为正确的最低有效测试层级，并在需要时组合单元、集成、契约、端到端和人工验证。
一致的测试策略可以让提交者解释验证边界，让评审者判断合并风险，并让例外具备可追踪记录。

<!-- anchor: scope -->
## 适用范围

适用于后端服务、前端应用、移动端/uni-app 页面、API 契约、消息契约、数据库迁移、配置变更、CI 验证、发布前验证和缺陷修复。
本规范覆盖测试层级选择、验证边界、缺失测试识别、人工验证记录和例外处理。
它不替代具体测试框架语法、覆盖率门禁或测试数据规则；这些内容应当由对应规则补充。

<!-- anchor: rules -->
## 规则

1. 每个功能、缺陷修复、重构或配置变更必须声明验证策略，说明使用的测试层级和未覆盖风险。
2. 单元测试必须覆盖纯业务规则、边界条件、错误分支和状态转换，并隔离不可控依赖。
3. 集成测试必须覆盖模块之间、数据库、缓存、消息、文件、配置解析和外部适配层的真实交互边界。
4. 契约测试必须覆盖提供方和消费方之间的 REST、AsyncAPI、gRPC 或 SDK 兼容性，不得用端到端测试替代契约兼容检查。
5. 端到端测试应当覆盖核心用户旅程和跨服务关键路径，不得用大量脆弱端到端测试替代更快、更明确的低层级测试。
6. 人工验证只可以用于视觉体验、探索性测试、临时环境限制或无法稳定自动化的场景，并必须记录步骤、环境、结果和剩余风险。
7. 评审中发现缺失测试时必须要求补充测试、调整测试层级或记录例外；不得只用“已手测”替代可自动化验证。
8. 测试例外必须说明原因、影响范围、替代验证、责任人或跟踪项，并在代码评审中显式确认后才能合并。

<!-- anchor: examples -->
## 示例

正例：

```yaml
change: add order cancellation
unit: validate status transitions and permission rules
integration: verify order row, inventory reservation, and message publish in one transaction
contract: verify cancellation API response and error shape
e2e: verify buyer cancels unpaid order from UI
manual: none
```

反例：

```yaml
change: add order cancellation
validation: clicked once locally
missing_tests: not recorded
exception: not recorded
```

<!-- anchor: checklist -->
## 检查清单

- 是否为变更声明单元、集成、契约、端到端和人工验证的边界。
- 是否把关键路径放在合适层级验证，而不是只依赖端到端或手工验证。
- 是否为缺陷修复补充回归测试。
- 是否识别数据库、消息、缓存、配置和外部接口的集成风险。
- 是否记录无法自动化的人工验证步骤、环境、结果和剩余风险。
- 是否在代码评审中处理缺失测试和测试例外。

<!-- anchor: relations -->
## 相关规范

- rules.test-coverage
- rules.contract-testing
- rules.test-data-mock
- rules.code-review

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
