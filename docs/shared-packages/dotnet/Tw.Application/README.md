# Tw.Application

`Tw.Application` 提供基于 MediatR 的应用用例执行管线。它将 MediatR 请求接入固定顺序的 `IApplicationPipelineBehavior`，并在 handler 成功后执行 `ICompletedHook`。

## DI 注册

```csharp
using Tw.Application.Pipeline;

services.AddApplicationPipeline(typeof(CreateOrderHandler).Assembly);
```

`AddApplicationPipeline` 会注册 MediatR，并添加 `MediatRApplicationPipelineBehavior<,>` 作为 open behavior。调用方通过 `IApplicationPipelineBehavior.Name` 参与固定顺序排序。

## 执行顺序

已知行为按以下顺序包裹 handler：

`ExecutionContext`、`Feature`、`Authorization`、`Validation`、`Idempotency`、`Sharding`、`Uow`、`Concurrency`、`Auditing`

如果注册了 `IValidator<TRequest>`，FluentValidation 会在 `Validation` 位置执行。未知名称排在已知行为之后。`ICompletedHook` 只在 handler 成功完成后执行。

## 注意事项

- handler 程序集必须显式传入 `AddApplicationPipeline`
- 自定义行为应当通过稳定 `Name` 加入固定顺序
- 验证失败会抛出 `FluentValidation.ValidationException`
- 本包不实现具体权限、Feature、UoW、审计存储或协议适配
