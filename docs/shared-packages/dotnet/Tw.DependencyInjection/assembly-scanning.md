# 程序集扫描与注册规划

## 能力定位

`Tw.DependencyInjection` 是容器中立的依赖注入运行时，引用 `Tw.DependencyInjection.Abstractions` 消费框架无关抽象。它提供程序集发现、白/黑名单过滤、依赖拓扑排序、循环诊断、服务注册规划和 `ServiceRegistrationReport` 诊断报告。服务注册仲裁详见 [服务自动注册](service-registration.md)；Options 自动装载详见 [配置与 Options 自动装载](options-binding.md)。

## 容器中立注册入口

在组合根中调用 `AddServiceRegistration(...)`，按 `Tw:DependencyInjection` 配置节执行扫描、规划和 Microsoft DI 注册：

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServiceRegistration(builder.Configuration);

var app = builder.Build();
app.Run();
```

该入口使用 Microsoft DI 默认容器完成服务注册。

## 扫描选项

扫描行为由 `ServiceRegistrationOptions` 控制，对应配置节 `Tw:DependencyInjection`：

| 选项 | 类型 | 用途 |
| --- | --- | --- |
| `IncludeAssemblies` | 字符串列表 | 在内置 `Tw.` 前缀之外精确补充纳入的程序集名 |
| `ExcludeAssemblies` | 字符串列表 | 精确排除的程序集名，优先于任何白名单 |
| `IncludeAssemblyPrefixes` | 字符串列表 | 叠加在内置 `Tw.` 前缀之上的额外白名单前缀 |
| `ExcludeAssemblyPrefixes` | 字符串列表 | 排除的程序集名前缀，优先于任何白名单 |

默认扫描运行时已加载程序集与依赖上下文中的 `Tw.` 前缀程序集。黑名单（`Exclude*`）优先于白名单。配置示例：

```json
{
  "Tw": {
    "DependencyInjection": {
      "IncludeAssemblyPrefixes": ["Acme."],
      "ExcludeAssemblies": ["Tw.Legacy"]
    }
  }
}
```

> 说明：程序集级优先级配置 `AssemblyPriorities` 属注册仲裁选项，见 [服务自动注册](service-registration.md)。

## 拓扑与诊断

扫描结果按程序集引用关系拓扑排序：被依赖程序集排在前、依赖方排在后，层级（`AssemblyTopologyEntry.Level`）随依赖深度递增。`ServiceRegistrationReport` 记录纳入扫描的程序集、被排除的程序集与拓扑层级。

## 注意事项

- 发现循环引用时启动失败，异常信息输出完整环路链路（如 `Tw.A -> Tw.B -> Tw.C -> Tw.A`）。
- 引擎只应由组合根引用；业务服务只依赖 `Tw.DependencyInjection.Abstractions`。
- 本包不接管其他容器，也不启用通用动态代理。
- 诊断报告只承载摘要元数据，不输出敏感配置值。
