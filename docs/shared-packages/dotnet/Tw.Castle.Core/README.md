# Tw.Castle.Core

`Tw.Castle.Core` 提供方法级拦截契约、拦截器选择、拦截执行管线、Castle DynamicProxy adapter 和拦截诊断。

## 能力

- 拦截执行管线：`IInterceptorPipeline`、`InterceptorPipeline`
- Attribute selector：`AttributeInterceptorSelector`、`[Intercept]`、`[DisableInterception]`、`[InterceptorOrder]`
- Castle adapter：`CastleAsyncInterceptorAdapter`、`CastleInvocationContext`
- 拦截诊断：`InterceptionReport`、`InterceptionDiagnostic`

## 使用文档

- [启用方法级动态代理拦截](method-interception.md)

## 边界

`Tw.Castle.Core` 不接管 Autofac 宿主构建，不执行 Autofac 服务注册，不注册 MVC filters，不注册 CAP filters，也不注册 gRPC interceptors。Autofac 集成由 [`Tw.DependencyInjection.Autofac`](../Tw.DependencyInjection.Autofac/README.md) 提供。MVC action 与 Razor Page adapter 由 [`Tw.AspNetCore.Mvc`](../Tw.AspNetCore.Mvc/README.md) 提供。
