namespace Tw.Identity.OpenIddict;

/// <summary>
/// OpenIddict 身份中心配置
/// </summary>
public sealed class OpenIddictIdentityOptions
{
    /// <summary>
    /// token 签发方
    /// </summary>
    public Uri? Issuer { get; set; }

    /// <summary>
    /// 允许的 token 受众集合
    /// </summary>
    public ISet<string> Audiences { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// 允许的 OAuth grant type 集合
    /// </summary>
    public ISet<string> AllowedGrantTypes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "authorization_code",
        "client_credentials",
        "refresh_token"
    };

    /// <summary>
    /// token 签名证书名称
    /// </summary>
    public string? SigningCertificateName { get; set; }

    /// <summary>
    /// authorization code flow 是否要求 PKCE
    /// </summary>
    public bool RequireProofKey { get; set; } = true;

    /// <summary>
    /// 校验 OpenIddict 身份中心配置
    /// </summary>
    /// <exception cref="InvalidOperationException">配置缺失或包含禁止的 password grant 时抛出</exception>
    public void Validate()
    {
        if (Issuer is null)
        {
            throw new InvalidOperationException("OpenIddict issuer is required");
        }

        if (Audiences.Count == 0)
        {
            throw new InvalidOperationException("OpenIddict token audience is required");
        }

        if (string.IsNullOrWhiteSpace(SigningCertificateName))
        {
            throw new InvalidOperationException("OpenIddict token signing certificate is required");
        }

        if (AllowedGrantTypes.Contains("password"))
        {
            throw new InvalidOperationException("OpenIddict password grant is not allowed");
        }
    }
}
