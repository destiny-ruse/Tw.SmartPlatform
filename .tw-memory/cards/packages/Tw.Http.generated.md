# Package: Tw.Http

标识：Tw.Http / backend/dotnet/BuildingBlocks/src/Http/Tw.Http / platform-team
职责：提供出站 HTTP 请求头名称、可信传播策略与安全选择边界。

适用范围：
- HTTP 请求头名称
- 出站请求头允许列表与可信级别
- 不可变请求头选择结果

不适用范围：
- ASP.NET Core 入站中间件
- 未经真实集成测试的 HttpClient 注册
- provider-neutral 韧性策略描述

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*, Polly*, Microsoft.Extensions.Http.Resilience*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Http
