using AwesomeAssertions;
using Xunit;

namespace Tw.Identity.OpenIddict.Tests;

/// <summary>
/// 覆盖开放Iddict身份选项的核心行为和边界条件
/// </summary>
public sealed class OpenIddictIdentityOptionsTests
{
    /// <summary>
    /// 验证校验拒绝缺少SigningCertificate
    /// </summary>
    [Fact]
    public void Validate_RejectsMissingSigningCertificate()
    {
        var options = new OpenIddictIdentityOptions
        {
            Issuer = new Uri("https://identity.smart-platform.local")
        };
        options.Audiences.Add("smart-platform-api");

        var act = options.Validate;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("OpenIddict token signing certificate is required");
    }

    /// <summary>
    /// 验证DefaultsDo不EnablePassword授权记录
    /// </summary>
    [Fact]
    public void Defaults_DoNotEnablePasswordGrant()
    {
        var options = new OpenIddictIdentityOptions();

        options.AllowedGrantTypes.Should().NotContain("password");
        options.AllowedGrantTypes.Should().Contain([
            "authorization_code",
            "client_credentials",
            "refresh_token"
        ]);
    }
}
