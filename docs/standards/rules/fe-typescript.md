# 前端 TypeScript 规范

## 目标

前端 TypeScript 必须把接口边界、状态边界和组件边界表达为可检查的类型，避免运行期才暴露字段缺失、空值误用和异步竞态。统一类型实践可以让页面、组件、请求层和测试代码在评审中以同一套规则判断风险。

## 适用范围

适用于 Vue TypeScript 项目中的 `src/**/*.ts`、`src/**/*.tsx`、`src/**/*.vue`、类型声明、API 请求封装、状态管理、路由守卫、测试代码和构建配置中的 TypeScript 片段。后端 DTO、数据库模型和非前端脚本不属于本规范范围，但前端消费这些契约时必须在边界处声明转换类型。

## 规则

1. API 响应、路由参数、组件 props、emits、状态对象和跨模块导出值必须声明明确类型，不得依赖隐式 `any`。
2. 前端边界类型应当区分传输层类型和视图层类型，接口字段转换必须集中在请求层、adapter 或 composable 中完成。
3. `null`、`undefined`、空数组和空字符串必须有一致语义；可选字段进入视图前应当完成默认值、保护分支或显式窄化。
4. 不得使用非空断言 `!` 绕过真实空值风险；只有框架生命周期或测试夹具已证明值存在时才可以使用，并应当有就近说明。
5. 异步 API 调用必须用 `try/catch`、结果对象或统一请求封装表达 loading、成功、失败和取消状态，不得让未处理 Promise 进入组件渲染路径。
6. 模块不得跨越所有权边界直接读取其他功能目录的内部文件；共享类型和工具必须通过该模块公开出口导出。
7. 泛型、联合类型和类型守卫应当表达真实约束，不得为了消除报错写过宽的 `Record<string, unknown>` 或类型断言。
8. 类型断言 `as` 必须靠近可信边界，并说明或验证来源；不得在业务逻辑深处反复断言同一对象形状。
9. 测试数据必须满足同一类型契约，可以使用 builder 或 fixture，不得用不完整对象强制断言通过编译。

## 示例

正例：

```typescript
type OrderDto = {
  id: string;
  paidAt?: string | null;
};

type OrderView = {
  id: string;
  paidAtText: string;
};

function toOrderView(dto: OrderDto, t: (key: string) => string): OrderView {
  return {
    id: dto.id,
    paidAtText: dto.paidAt ? formatDate(dto.paidAt) : t("order.status.unpaid"),
  };
}

const emit = defineEmits<{
  saved: [orderId: string];
  failed: [message: string];
}>();
```

反例：

```typescript
const detail = (await request("/orders/1")) as any;
const paidAtText = formatDate(detail.paidAt!);

// 直接读取其他功能目录内部实现。
import { normalizeOrder } from "@/features/order/internal/normalize";
```

## 检查清单

- API、props、emits、路由参数和状态是否都有明确类型。
- 传输层类型是否在清晰边界转换为视图层类型。
- 空值语义是否一致，并在进入模板前完成保护或默认值处理。
- 异步调用是否覆盖 loading、错误、取消或重试等实际状态。
- 是否避免跨模块读取内部文件、滥用 `any`、`as` 和非空断言。
- 测试 fixture 是否符合真实类型契约。

## 相关规范

- rules.naming-ts-vue
- rules.fe-vue-ts-project
- rules.api-response-shape
- rules.input-validation

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
