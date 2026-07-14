# Tw.Gateway.Yarp

`Tw.Gateway.Yarp` 是实验阶段的 YARP 适配包，当前仅提供路由约束校验和请求头转换边界，不提供完整宿主集成。

## 校验网关路由

在把 `GatewayRoute` 转换为 YARP 路由配置前执行校验：

```csharp
using Tw.Gateway.Yarp;

YarpRouteValidation.Validate(route);
```

严格全局限流与网关本地限流同时启用时，校验会抛出 `InvalidOperationException`。

## DI 注册

本包没有 `IServiceCollection` 注册入口。宿主必须直接使用 YARP 官方注册 API 完成反向代理、服务发现和配置源装配，不能把引用本包视为已启用网关运行时。

## 注意事项

- 包稳定性为 `experimental`
- `YarpHeaderTransformFactory` 尚不构成完整的转换器注册能力
- 服务发现绑定、健康检查、超时、失败语义与真实提供方集成测试不在当前能力范围内
