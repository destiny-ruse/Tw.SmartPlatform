# Tw.ExceptionHandling

`Tw.ExceptionHandling` 提供稳定错误描述、默认异常分类映射，以及位于 `Tw.ExceptionHandling.Validation` 命名空间的结构化输入验证错误。

## 映射输入验证错误

创建包含字段路径、稳定错误码和安全消息的错误集合，再交给默认映射器：

```csharp
using Tw.ExceptionHandling;
using Tw.ExceptionHandling.Validation;

var exception = new ValidationException(
[
    new ValidationError("order.lines[0].quantity", "VALIDATION:RANGE", "数量必须大于零")
]);

var descriptor = new DefaultExceptionToErrorMapper().Map(exception);
```

映射结果使用 `VALIDATION:000001`、`ErrorCategory.Validation` 和消息 `输入验证失败`。`ValidationErrors` 按原顺序保留每项错误的 `FieldPath`、`Code` 与 `Message`。

## DI 注册

本包只提供模型与无状态映射器，不提供 `IServiceCollection` 注册入口。宿主可以直接创建 `DefaultExceptionToErrorMapper`，也可以按自身生命周期要求注册 `IExceptionToErrorMapper`。

## 注意事项

- `ValidationException` 在构造时复制错误集合，并通过只读集合公开快照
- 未知异常统一映射为 `SYSTEM:999999` 和安全消息 `系统异常`
- HTTP、gRPC、CAP 等协议状态与响应结构由各入口适配包负责
- 本包不负责记录异常日志，也不定义业务异常继承层级
