---
id: rules.naming-uniapp
title: uni-app 命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [uniapp]
tags: [frontend, uniapp, cross-platform]
summary: 规定 uni-app 页面、组件、平台条件和资源命名方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# uni-app 命名规范

<!-- anchor: goal -->
## 目标

统一 uni-app 命名方式，让页面、组件、资源和平台能力在多端构建中可搜索、可评审、可追踪。命名必须减少路径冲突、端专属资源误用和条件编译块难以定位的风险。

<!-- anchor: scope -->
## 适用范围

适用于 uni-app 项目的页面目录、组件、组合式函数、API 封装、平台 adapter、资源文件、样式文件、`pages.json` 路由路径、测试和端专属配置命名。第三方平台返回字段和外部协议字段不属于本规范范围；展示或存入前端领域模型前应当转换为项目命名。

<!-- anchor: rules -->
## 规则

1. 页面目录和路由路径必须使用稳定的 kebab-case，并表达业务场景，例如 `order-detail`。
2. 页面入口文件应当遵循项目约定并与目录语义一致，不得用 `page1`、`new-page` 等临时名称进入提交。
3. 组件名必须使用 PascalCase，组件文件或目录名称必须能表达复用场景，例如 `OrderCard`。
4. 组合式函数必须使用 `use` + camelCase，并表达跨端能力或业务状态，例如 `useDeviceInfo`。
5. 平台适配文件应当包含能力或平台语义，例如 `share-adapter.ts`、`wechat-login.ts`，不得使用 `utils.ts` 承载多端差异。
6. 资源文件必须使用 kebab-case，并按领域、用途或平台分组；端专属资源必须在名称或目录中体现目标端。
7. `pages.json` 中的路径、页面标题键和组件引用名称必须与实际目录和语言键保持一致。
8. 缩写必须来自团队词表、平台官方缩写或行业通用词，不得创造只有当前模块理解的短名。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 页面目录 | kebab-case | `order-detail` |
| 组件 | PascalCase | `OrderCard` |
| 组合式函数 | useCamelCase | `useDeviceInfo` |
| 平台适配文件 | kebab-case + adapter 语义 | `share-adapter.ts` |
| 资源文件 | kebab-case | `empty-state.png` |

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "path": "pages/order-detail/index",
  "style": {
    "navigationBarTitleText": "%order.detail.title%"
  }
}
```

```typescript
import { useDeviceInfo } from "@/composables/useDeviceInfo";
import { shareToPlatform } from "@/platform/share-adapter";
```

反例：

```text
pages/newPage/index.vue
components/Card2.vue
static/img/a.png
platform/utils.ts
```

<!-- anchor: checklist -->
## 检查清单

- 页面目录、路由路径和 `pages.json` 是否使用稳定业务命名。
- 组件、组合式函数和平台 adapter 是否从名称体现职责。
- 资源是否按领域、用途或平台分组并使用 kebab-case。
- 端专属文件或资源是否能从名称或目录识别目标端。
- 是否避免临时编号、万能 `utils`、无意义缩写和路径大小写混乱。
- 外部字段进入前端领域模型前是否完成命名转换。

<!-- anchor: relations -->
## 相关规范

- rules.naming-common
- rules.fe-uniapp-cross
- rules.fe-compat

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
