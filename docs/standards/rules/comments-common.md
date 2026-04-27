---
id: rules.comments-common
title: 通用注释规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: []
tags: [comments, governance]
summary: 规定跨语言注释的意图、准确性和维护要求。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 通用注释规范

<!-- anchor: goal -->
## 目标

通用注释规范用于降低注释与代码不一致、复杂逻辑缺少背景、公共 API 语义不清和技术债无法追踪的风险。注释必须补充代码无法直接表达的意图、约束和取舍，而不是重复表面行为。统一注释标准后，评审者和维护者可以判断哪些说明需要保留、更新或删除。

<!-- anchor: scope -->
## 适用范围

本规范适用于源码、测试、配置示例、公共 API 文档注释、迁移脚本、部署说明中的行内注释和块注释。它不适用于自动生成代码中的工具注释、第三方 vendored 文件、临时本地草稿或由外部协议原样生成的文档内容。

<!-- anchor: rules -->
## 规则

1. 注释必须解释代码本身看不出的意图、业务约束、兼容性原因、安全边界或性能取舍；不得复述语法动作。
2. 公共 API、共享库入口和跨模块调用点必须说明输入、输出、异常、错误语义或副作用中的关键约束；不得让调用方通过阅读实现猜测契约。
3. 注释必须随代码同步维护；发现注释与实现冲突时必须更新或删除注释，不得保留过期说明。
4. 注释不得掩盖过度复杂的实现；当命名、拆分函数或重构结构能表达意图时，应当优先改代码。
5. TODO、FIXME、HACK 等待办注释必须说明原因、边界和可追踪事项；不得留下无责任上下文的永久待办。
6. 安全、隐私、兼容性和数据迁移注释必须说明触发条件和不能随意修改的原因；不得只写“不要改”。
7. 测试注释应当说明场景、风险或特殊夹具原因；不得逐行解释断言语法。
8. 生成代码边界必须尊重工具约定；不得在 generated 文件中添加需要长期维护的人工注释。

<!-- anchor: examples -->
## 示例

正例：

```typescript
// 保留 legacyCode 字段以兼容仍使用 2025 版协议的移动端客户端。
const legacyCode = response.code;
```

反例：

```typescript
// i 加 1
i++;
```

<!-- anchor: checklist -->
## 检查清单

- 注释是否解释原因、约束、风险或契约，而不是复述代码。
- 公共 API 和共享入口是否说明调用方需要知道的输入、输出、异常或副作用。
- 注释是否与当前实现、测试和配置保持一致。
- TODO、FIXME 或 HACK 是否有可追踪上下文和边界。
- 是否可以通过更好的命名或结构替代注释。
- generated 文件中是否避免新增人工维护注释。

<!-- anchor: relations -->
## 相关规范

- rules.code-review
- rules.naming-common

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
