# Vue TypeScript 项目规范

## 目标

Vue TypeScript 项目必须让页面、组件、组合式函数、请求层和构建配置形成清晰边界，避免功能目录互相穿透和组件职责膨胀。统一项目结构可以让新增页面、迁移模块和自动化修改都落在可评审的位置。

## 适用范围

适用于 Vue TypeScript 项目的 `src/pages`、`src/views`、`src/components`、`src/features`、`src/composables`、`src/router`、`src/stores`、`src/api`、测试目录和 Vite、TypeScript、ESLint 等前端配置。纯后端服务、uni-app 专属目录和一次性脚本不属于本规范范围；共享包被 Vue 项目消费时只按其公开 API 评审。

## 规则

1. 页面组件必须负责路由级编排，领域计算、请求转换和复用交互应当下沉到 feature、composable 或 service。
2. 组件 props 和 emits 必须使用 TypeScript 声明，公开组件应当保持小而稳定的输入输出契约。
3. 组件不得直接修改 props、全局对象或其他模块内部状态；跨组件通信必须通过 emits、store、路由或明确的服务接口完成。
4. 组合式函数必须封装一个可复用行为或状态边界，不得成为任意工具函数集合。
5. 路由记录必须有稳定名称、路径参数类型约束和必要的 meta 字段，不得在页面内散落硬编码路由字符串。
6. 功能模块应当通过公开出口暴露组件、类型和服务，不得从其他模块 import `internal`、私有目录或页面文件。
7. 构建、别名和环境变量配置必须集中管理，页面和组件不得直接读取未声明的运行时变量。
8. 单文件组件应当保持模板、脚本和样式职责清晰；当文件承担多个业务流程时必须拆分组件或 composable。

## 示例

正例：

```vue
<script setup lang="ts">
type Props = {
  orderId: string;
};

const props = defineProps<Props>();
const emit = defineEmits<{
  saved: [orderId: string];
}>();

const { order, saveOrder } = useOrderEditor(props.orderId);
</script>
```

反例：

```typescript
// 页面跨模块读取内部实现，后续模块重构会破坏调用方。
import { createOrderDraft } from "@/features/order/internal/draft";

router.push("/order/detail/" + id);
```

## 检查清单

- 页面、组件、composable、store 和 API 层职责是否清楚。
- props、emits、路由参数和公开导出是否具备稳定类型。
- 是否通过模块公开出口访问能力，而不是穿透私有目录。
- 路由名称、meta 和跳转是否集中可搜索。
- 构建配置、别名和环境变量是否集中声明。
- 大组件是否已拆分出复用组件或组合式函数。

## 相关规范

- rules.fe-typescript
- rules.naming-ts-vue
- rules.fe-styles
- rules.comments-ts

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
