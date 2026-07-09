using AwesomeAssertions;
using Xunit;

namespace Tw.Identity.OpenIddict.Tests;

/// <summary>验证 OpenIddictIdentityOptionsTests 相关行为</summary>
public sealed class OpenIddictIdentityOptionsTests
{
    /// <summary>验证 Validate_RejectsMissingSigningCertificate 场景</summary>
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

    /// <summary>验证 Defaults_DoNotEnablePasswordGrant 场景</summary>
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
