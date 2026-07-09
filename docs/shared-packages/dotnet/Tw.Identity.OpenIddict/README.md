# Tw.Identity.OpenIddict

`Tw.Identity.OpenIddict` 提供基于 OpenIddict 的 Identity Center 边界注册、配置校验、token 发行契约、token 校验契约和签名证书解析契约。

## DI 注册

```csharp
using Tw.Identity.OpenIddict;

services.AddScoped<IIdentityTokenIssuer, HostTokenIssuer>();
services.AddScoped<IIdentityTokenValidator, HostTokenValidator>();
services.AddScoped<IIdentitySigningCertificateResolver, HostSigningCertificateResolver>();

services.AddIdentityOpenIddict(options =>
{
    options.Issuer = new Uri("https://identity.smart-platform.local");
    options.Audiences.Add("smart-platform-api");
    options.SigningCertificateName = "smart-platform-token-signing";
});
```

`AddIdentityOpenIddict` 会校验 issuer、audience、signing certificate name，并注册 OpenIddict server 与 validation 组件。默认启用 authorization code、client credentials 和 refresh token flow，不启用 password grant。

## 注意事项

- 默认 token issuer、validator 和 signing certificate resolver 是宿主占位边界，会抛出 `NotSupportedException`
- Identity Center 宿主必须在调用 `AddIdentityOpenIddict` 前注册真实 adapter；本包使用 `TryAddScoped`，不会覆盖宿主实现
- 签名证书名称不得是证书内容、私钥或密钥值
- 本包不实现用户管理、客户端管理、权限 grant 存储或业务权限判断
