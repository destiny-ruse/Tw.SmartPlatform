---
id: rules.naming-ts-vue
title: TypeScript 与 Vue 命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [vue-ts]
tags: [frontend, vue-ts, code-style]
summary: 规定 TypeScript、Vue 组件、组合式函数和路由命名方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# TypeScript 与 Vue 命名规范

<!-- anchor: goal -->
## 目标

统一 TypeScript 与 Vue 命名方式，让代码在搜索、评审和跨团队协作中保持可读。命名必须暴露领域概念、职责边界和文件用途，降低重复概念、错误 import 和路由跳转失配的风险。

<!-- anchor: scope -->
## 适用范围

适用于 Vue TypeScript 项目的类型、接口、枚举、变量、函数、组件、组合式函数、路由名称、store、文件、测试和配置命名。第三方库原始字段、后端固定协议字段和迁移期兼容字段可以保留外部命名，但进入前端领域模型时应当转换为本规范命名。

<!-- anchor: rules -->
## 规则

1. 命名必须表达领域含义和职责边界，不得使用 `data1`、`obj`、`tmpValue`、`CommonHelper` 等无业务含义名称。
2. 同一概念在同一模块和相邻模块中必须使用同一词汇，不得混用 `order`、`trade`、`bill` 表达同一对象。
3. Vue 组件文件和组件名必须使用 PascalCase，并优先使用名词或名词短语，例如 `OrderDetailPanel.vue`。
4. 组合式函数必须使用 `use` + Pascal 语义的 camelCase，例如 `useOrderQuery`，且名称必须表达封装的状态或行为。
5. 类型、接口、枚举和类必须使用 PascalCase；布尔变量和返回布尔值的函数应当使用 `is`、`has`、`can`、`should` 等前缀表达判断语义。
6. 路由名称必须稳定、唯一且可搜索，应当使用领域 + 动作或视图名称，例如 `order-detail` 或 `OrderDetail`，同一项目内不得混用两种风格。
7. 事件名和 emits payload 名称必须表达业务事件，不得使用 `change` 承载多个含义；通用输入组件可以使用框架约定事件。
8. 缩写必须来自团队词表或行业通用词；不得创造只有当前作者理解的缩写。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 组件文件 | PascalCase | `OrderList.vue` |
| 组合式函数 | useCamelCase | `useOrderQuery` |
| 变量/函数 | camelCase | `selectedOrder` |
| 布尔变量 | is/has/can/should + camelCase | `canSubmit` |
| 类型/接口 | PascalCase | `OrderItem` |
| 路由名称 | 项目内统一风格 | `order-detail` |

<!-- anchor: examples -->
## 示例

正例：

```typescript
type OrderDetail = {
  id: string;
  canRefund: boolean;
};

const selectedOrder = ref<OrderDetail | null>(null);
const routeName = "order-detail";
const { refreshOrders } = useOrderQuery();
```

反例：

```typescript
const obj = ref(null);
const Manager2 = defineComponent({});
const { run } = useCommon();
router.push({ name: "detail" });
```

<!-- anchor: checklist -->
## 检查清单

- 是否能从名称判断领域对象、职责和使用场景。
- 同一概念是否在类型、变量、组件、路由和事件中保持同一词汇。
- 组件、组合式函数、类型和布尔值是否符合命名形态。
- 路由名称是否稳定、唯一、可搜索。
- 是否避免无意义缩写、编号、万能后缀和过宽名称。
- 外部协议字段是否在进入前端模型时完成必要命名转换。

<!-- anchor: relations -->
## 相关规范

- rules.naming-common
- rules.fe-typescript
- rules.fe-vue-ts-project

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
