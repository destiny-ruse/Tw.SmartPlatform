# Tw.Observability.OpenTelemetry

`Tw.Observability.OpenTelemetry` 是实验阶段的配置模型包，当前提供 OpenTelemetry 与 Aspire Dashboard 选项，不提供完整遥测注册。

## 使用配置模型

`OpenTelemetryRegistrationOptions.Default` 默认不启用 gRPC .NET 客户端插桩。`AspireDashboardOptions` 用于表达可选的 OTLP 端点：

```csharp
using Tw.Observability.OpenTelemetry;

var registration = OpenTelemetryRegistrationOptions.Default;
var dashboard = new AspireDashboardOptions("http://localhost:18889");
```

## DI 注册

本包没有 `IServiceCollection` 注册入口。宿主必须直接使用 OpenTelemetry 官方 API 配置 tracing、metrics、OTLP 导出器和具体插桩，不能把引用本包视为已启用遥测。

## 注意事项

- 包稳定性为 `experimental`
- 当前配置模型不会自动注册服务、导出器或插桩
- gRPC 客户端插桩默认关闭
- 真实提供方集成、失败语义、健康检查和运行验证不在当前能力范围内
