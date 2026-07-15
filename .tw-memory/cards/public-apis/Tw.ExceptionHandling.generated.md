# Public API: Tw.ExceptionHandling

标识：Tw.ExceptionHandling / backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling

公开能力边界：
- Tw.ExceptionHandling
- Tw.ExceptionHandling.Validation

实现公开命名空间：
- Tw.ExceptionHandling
- Tw.ExceptionHandling.Validation

公开类型：
- sealed class DefaultExceptionToErrorMapper - Tw.ExceptionHandling (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/DefaultExceptionToErrorMapper.cs:8)
- enum ErrorCategory - Tw.ExceptionHandling (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/ErrorDescriptor.cs:8)
- sealed record ErrorDescriptor - Tw.ExceptionHandling (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/ErrorDescriptor.cs:54)
- interface IExceptionToErrorMapper - Tw.ExceptionHandling (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/IExceptionToErrorMapper.cs:6)
- sealed record ValidationError - Tw.ExceptionHandling.Validation (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/Validation/ValidationError.cs:9)
- sealed class ValidationException - Tw.ExceptionHandling.Validation (backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/Validation/ValidationException.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.ExceptionHandling/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.ExceptionHandling
