# Package: Tw.ExceptionHandling

标识：Tw.ExceptionHandling / backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling / platform-team
职责：提供稳定错误描述、结构化输入验证错误和默认异常分类映射。

适用范围：
- 错误描述模型
- 错误分类枚举
- 结构化输入验证错误
- 输入验证异常
- 异常映射接口
- 默认异常映射器

不适用范围：
- HTTP 错误响应格式
- 日志记录实现
- 业务异常层级治理

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.ExceptionHandling
