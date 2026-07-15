# Public API: Tw.Identity.OpenIddict

标识：Tw.Identity.OpenIddict / backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict

公开能力边界：
- Tw.Identity.OpenIddict

实现公开命名空间：
- Tw.Identity.OpenIddict

公开类型：
- interface IIdentitySigningCertificateResolver - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentitySigningCertificateResolver.cs:8)
- interface IIdentityTokenIssuer - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentityTokenIssuer.cs:6)
- interface IIdentityTokenValidator - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IIdentityTokenValidator.cs:6)
- sealed record IdentityTokenRequest - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenRequest.cs:9)
- sealed record IdentityTokenValidationRequest - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenValidationRequest.cs:8)
- sealed record IdentityTokenValidationResult - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/IdentityTokenValidationResult.cs:10)
- sealed class OpenIddictIdentityOptions - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/OpenIddictIdentityOptions.cs:6)
- static class OpenIddictIdentityServiceCollectionExtensions - Tw.Identity.OpenIddict (backend/dotnet/BuildingBlocks/src/Application/Tw.Identity.OpenIddict/OpenIddictIdentityServiceCollectionExtensions.cs:9)

DI 注册入口：
- Tw.Identity.OpenIddict.OpenIddictIdentityServiceCollectionExtensions.AddIdentityOpenIddict

包参考文档：
- docs/shared-packages/dotnet/Tw.Identity.OpenIddict/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Identity.OpenIddict
