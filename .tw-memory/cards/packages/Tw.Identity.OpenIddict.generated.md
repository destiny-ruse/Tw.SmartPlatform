# Package: Tw.Identity.OpenIddict

标识：Tw.Identity.OpenIddict / backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict / platform-team
职责：基于 OpenIddict 的 Identity Center 边界、token 发行、token 校验和签名证书解析。

适用范围：
- OpenIddict 配置入口
- token 发行边界
- token 校验边界
- 签名证书解析边界

不适用范围：
- 用户管理 UI
- 权限 grant 存储
- 业务权限检查执行

依赖边界：
- forbid: SqlSugar*, Yarp*
- allow: OpenIddict, OpenIddict.Server.AspNetCore, OpenIddict.Validation.AspNetCore, OpenIddict.Validation.ServerIntegration, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Options

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Identity.OpenIddict
