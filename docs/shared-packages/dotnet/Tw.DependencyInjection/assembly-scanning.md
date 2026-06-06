# 程序集扫描与容器接管

## 能力定位

`Tw.DependencyInjection` 是依赖注入执行引擎包，引用 `Tw.Core` 消费其框架无关抽象，直接引用 Autofac 执行容器接管。P1 阶段提供扫描地基：程序集发现、白/黑名单过滤、依赖拓扑排序与循环诊断、`UseAutofac()` 容器接管，以及 `ServiceRegistrationReport` 诊断骨架。服务注册仲裁、Options 装载与 AOP 承载属于后续阶段。

## 容器接管

在宿主构建阶段用 `UseAutofac()` 接管默认依赖注入容器：

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();

var app = builder.Build();
app.Run();
```

`UseAutofac()` 是 `IHostBuilder` 扩展，内部委托 `AutofacServiceProviderFactory`，接管后 `host.Services` 为 Autofac 服务提供程序。

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

> 说明：把配置节绑定到 `ServiceRegistrationOptions` 并驱动注册的入口 `AddServiceRegistration(IConfiguration)` 随 DI 注册阶段（P2）落地。

## 拓扑与诊断

扫描结果按程序集引用关系拓扑排序：被依赖程序集排在前、依赖方排在后，层级（`AssemblyTopologyEntry.Level`）随依赖深度递增。`ServiceRegistrationReport` 记录纳入扫描的程序集、被排除的程序集与拓扑层级。

## 注意事项

- 发现循环引用时启动失败，异常信息输出完整环路链路（如 `Tw.A -> Tw.B -> Tw.C -> Tw.A`）。
- 引擎只应由组合根（宿主启动）引用；业务服务只依赖 `Tw.Core` 抽象。
- 诊断报告只承载摘要元数据，不输出敏感配置值。
