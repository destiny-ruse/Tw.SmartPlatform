---
id: rules.comments-dotnet
title: .NET 注释规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, ai]
stacks: [dotnet]
tags: [comments, dotnet, code-style]
summary: 规定 .NET XML 注释、公共 API 说明和复杂逻辑注释方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# .NET 注释规范

<!-- anchor: goal -->
## 目标

.NET 注释规范用于降低公共类型缺少 XML 契约、异步和异常语义不清、兼容性逻辑难以维护的风险。注释应当配合 C# 类型系统表达调用约束，让库、服务和测试在 IDE、文档生成和评审中保持一致。复杂实现的说明必须聚焦意图和边界。

<!-- anchor: scope -->
## 适用范围

本规范适用于 .NET 项目的 C# 源码、公共类型、接口、扩展方法、控制器、配置绑定类型、测试夹具和示例代码中的 XML 注释与行内注释。它不适用于工具生成的 `.g.cs`、外部包源码、临时调试片段或由 OpenAPI 等工具覆盖生成的客户端文件。

<!-- anchor: rules -->
## 规则

1. 公共类型、公共成员、接口方法和跨程序集可见 API 必须使用 XML 注释说明用途和关键契约；不得依赖实现细节作为唯一说明。
2. XML 注释必须使用 `<summary>` 表达职责，必要时使用 `<param>`、`<returns>`、`<exception>`、`<remarks>` 补充输入、输出、异常和副作用。
3. 异步方法注释应当说明取消、重试、幂等性或外部调用边界；不得只写“异步执行”。
4. 抛出业务异常、验证异常或外部依赖异常的公共 API 必须说明调用方可预期的异常类型或错误语义。
5. 行内注释必须解释兼容性、安全、性能或迁移取舍；不得注释普通 C# 语句的语法行为。
6. `<inheritdoc />` 可以用于继承稳定接口契约，但实现改变副作用、异常或性能特征时必须补充说明。
7. 测试代码注释应当说明特殊数据、时间控制或外部依赖替身的原因；不得逐行解释 Arrange、Act、Assert。
8. 生成代码、迁移快照和设计器文件不得添加需要人工长期维护的注释；相关说明应当写在源模板或相邻文档中。

<!-- anchor: examples -->
## 示例

正例：

```csharp
/// <summary>Schedules an order cancellation when the order is still reversible.</summary>
/// <param name="orderId">Stable order identifier from the public API.</param>
/// <exception cref="InvalidOperationException">Thrown when the order has already been settled.</exception>
public Task CancelAsync(string orderId, CancellationToken cancellationToken);
```

反例：

```csharp
// 循环订单
foreach (var order in orders)
{
    // 添加订单
    result.Add(order);
}
```

<!-- anchor: checklist -->
## 检查清单

- 公共类型、接口和成员是否有必要 XML 注释。
- XML 注释是否覆盖调用方需要知道的参数、返回、异常或副作用。
- 异步、取消、重试和外部调用边界是否说明清楚。
- 行内注释是否解释意图或约束，而不是复述 C# 语法。
- `<inheritdoc />` 是否只用于契约完全一致的实现。
- generated 文件中是否避免人工维护注释。

<!-- anchor: relations -->
## 相关规范

- rules.comments-common
- rules.naming-dotnet

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
