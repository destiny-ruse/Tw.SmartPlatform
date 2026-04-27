---
id: rules.naming-dotnet
title: .NET 命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, ai]
stacks: [dotnet]
tags: [naming, dotnet, code-style]
summary: 规定 .NET 项目的类型、成员、异步方法和配置命名方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# .NET 命名规范

<!-- anchor: goal -->
## 目标

.NET 命名规范用于降低命名空间漂移、类型职责不清、异步方法误用和配置键难以关联代码的风险。命名必须符合 C# 与 .NET 生态约定，同时表达本项目领域语言。统一命名后，IDE 搜索、公共 API 文档和跨服务协作可以保持一致。

<!-- anchor: scope -->
## 适用范围

本规范适用于 .NET 项目的命名空间、程序集、类型、接口、枚举、方法、属性、字段、参数、局部变量、异步方法、测试类型、配置绑定类和文件命名。它不适用于工具生成的 `.g.cs`、外部包源码、序列化时必须保留的外部字段名或迁移期兼容字段。

<!-- anchor: rules -->
## 规则

1. 命名空间和程序集必须使用 PascalCase 分段，并与目录、模块和领域边界一致；不得使用含糊的 `Common`、`Utils` 作为主要领域边界。
2. 类型、枚举、属性、事件和公共方法必须使用 PascalCase；局部变量和参数必须使用 camelCase；私有字段应当使用 `_camelCase`。
3. 接口名称必须以 `I` 开头并表达能力或角色，例如 `IOrderRepository`；不得使用 `IManager`、`IHelper` 等宽泛名称。
4. 异步方法必须以 `Async` 结尾，并返回 `Task`、`Task<T>`、`ValueTask` 或等效异步类型；不得给同步方法添加 `Async` 后缀。
5. 布尔属性和方法应当使用 `Is`、`Has`、`Can`、`Should` 等判断语义；不得使用 `Flag` 或 `StatusBool`。
6. 常量必须表达不可变业务含义，使用 PascalCase 或项目约定的 .NET 常量风格；不得把运行时配置伪装成常量。
7. 测试类型和方法名称必须表达被测行为和期望结果；不得只使用 `Test1`、`ShouldWork`。
8. 配置绑定类和配置节名称必须能从代码定位到配置文件路径；外部配置键名进入 C# 类型时应当转换为 .NET 命名风格。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 命名空间 | PascalCase 分段 | `Tw.Ordering.Application` |
| 类型/属性/方法 | PascalCase | `OrderService` |
| 异步方法 | PascalCase + Async | `SubmitAsync` |
| 局部变量/参数 | camelCase | `orderId` |
| 私有字段 | _camelCase | `_clock` |

<!-- anchor: examples -->
## 示例

正例：

```csharp
public interface IOrderRepository
{
    Task<Order?> FindByIdAsync(string orderId, CancellationToken cancellationToken);
}
```

反例：

```csharp
public class CommonHelper2
{
    public bool Flag;
    public Order GetAsync(string id) => new();
}
```

<!-- anchor: checklist -->
## 检查清单

- 命名空间、目录、程序集和领域边界是否一致。
- 类型、接口、成员、字段、参数和局部变量是否符合 .NET 命名形态。
- 异步方法是否只在真实异步 API 上使用 `Async` 后缀。
- 布尔值、测试名称和配置绑定名称是否表达真实语义。
- 是否避免 `Common`、`Helper`、`Manager`、编号和无意义缩写。
- generated 或外部协议名称是否被隔离或转换。

<!-- anchor: relations -->
## 相关规范

- rules.naming-common
- rules.comments-dotnet

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
