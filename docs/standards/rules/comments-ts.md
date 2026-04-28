---
id: rules.comments-ts
title: TypeScript 注释规范
doc_type: rule
status: active
version: 1.2.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [vue-ts]
tags: [frontend, vue-ts, code-style]
summary: 规定 TypeScript、Vue 和前端契约注释方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# TypeScript 注释规范

<!-- anchor: goal -->
## 目标

让 TypeScript 注释解释意图、约束和取舍，而不是重复代码表面行为。注释必须降低 API 契约、兼容分支和复杂业务规则的误读风险，使维护者在评审和重构时能判断哪些行为不能随意改变。

<!-- anchor: scope -->
## 适用范围

适用于 `src/**/*.ts`、`src/**/*.tsx`、`src/**/*.vue`、测试代码、类型声明、配置示例和公共 API 文档注释。公共组件、组合式函数、导出的类型、请求封装、兼容性分支和临时迁移逻辑均在本规范范围内；纯局部变量、显而易见的表达式和可通过命名直接表达的逻辑不应依赖注释。

<!-- anchor: rules -->
## 规则

1. 注释必须解释代码无法直接表达的意图、业务约束、兼容原因、安全注意事项或历史迁移背景。
2. 注释不得复述语句表面行为，例如在 `count++` 上方写“数量加一”。
3. TSDoc、单行注释和块注释中的自然语言必须使用简体中文；代码标识符、协议名、英文缩写、标准格式和外部契约字段可以保留原文。
4. TSDoc 正文、`@param`、`@returns`、属性描述、props 描述和单行注释末尾不得使用中英文句号。
5. 类型字段、组件 props、emits、ref 状态和访问器注释必须直接描述成员含义；不得包含“获取”“设置”“获取或设置”等模板化字样。
6. 单行注释必须简洁说明意图，优先使用 `// ` 放在被说明代码上方并与代码对齐；注释与代码之间不留空行，注释上方保留空行，代码块第一行除外。
7. 除导出 API 的 TSDoc 外，函数内部的复杂算法、业务流程、规避已知缺陷的 Hack 和非显而易见分支必须添加注释，说明为什么这样处理。
8. 导出的函数、组合式函数、类型、组件 props 和 emits 的注释应当说明输入边界、返回值、异常条件、副作用和调用时机。
9. 对异步 API 调用的注释必须说明重试、取消、幂等、缓存或错误吞吐策略中至少一项实际存在的约束。
10. 对 `null`、`undefined`、空数组、空字符串等特殊值的注释必须说明其业务含义，不得只写“可能为空”。
11. TODO、FIXME、临时兼容注释必须包含可追踪条件，例如版本、接口字段、缺陷编号或删除触发条件。
12. 注释与代码不一致时必须先更新或删除注释，不得让过期注释留在提交中。
13. 复杂逻辑应当优先通过拆分函数、命名类型或显式状态表达；只有仍需说明取舍时才可以增加注释。

<!-- anchor: examples -->
## 示例

正例：

```typescript
/**
 * 保留 legacyStatus 是为了兼容仍返回旧字段的移动端 WebView
 * 当 /orders/detail 响应稳定只返回 status 后可以删除
 */
export interface OrderStatusPayload {
  status?: OrderStatus;
  legacyStatus?: OrderStatus;
}

// undefined 表示尚未请求，null 表示接口确认无当前用户
const currentUser = ref<User | null | undefined>(undefined);
```

反例：

```typescript
// 调用接口
const result = await queryOrder();

// i 加 1
i++;
```

<!-- anchor: checklist -->
## 检查清单

- 注释是否解释了代码看不出的意图、约束或取舍。
- 公共 API、props、emits、导出类型和组合式函数是否说明了调用边界。
- 注释中的自然语言是否使用简体中文。
- TSDoc、参数描述、props 描述和单行注释末尾是否避免中英文句号。
- 类型字段、props、emits、ref 状态和访问器注释是否避免“获取”“设置”“获取或设置”。
- 函数内部复杂逻辑、兼容分支或 Hack 是否说明原因。
- 空值、异步调用和兼容分支是否写明业务含义或删除条件。
- TODO、FIXME 和临时注释是否可追踪、可删除。
- 注释是否与当前代码、类型和示例保持一致。
- 是否避免用注释掩盖可通过命名或结构改善的复杂度。

<!-- anchor: relations -->
## 相关规范

- rules.comments-common
- rules.fe-typescript
- rules.fe-vue-ts-project
- rules.naming-ts-vue

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.2.0 | 2026-04-28 | 补充 TypeScript 与 Vue 注释语言、句号、成员注释和函数内部复杂逻辑注释格式要求。 |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
