# Package: Tw.Resilience

标识：Tw.Resilience / backend/dotnet/BuildingBlocks/src/Resilience/Tw.Resilience / platform-team
职责：提供 provider-neutral 操作分类、韧性策略描述、输入校验与重试安全规则。

适用范围：
- 操作幂等分类
- 公司自有韧性策略描述
- 超时与重试配置校验
- 非幂等写入的自动重试禁用规则

不适用范围：
- 具体 HTTP handler 与 HttpClient 注册
- Polly 或其他第三方 provider 集成
- 业务降级行为

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*, Polly*, Microsoft.Extensions.Http.Resilience*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Resilience
