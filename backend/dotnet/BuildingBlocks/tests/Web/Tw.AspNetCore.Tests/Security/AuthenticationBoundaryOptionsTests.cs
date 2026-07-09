using AwesomeAssertions;
using Tw.AspNetCore.Security;
using Xunit;

namespace Tw.AspNetCore.Tests.Security;

/// <summary>验证 AuthenticationBoundaryOptionsTests 相关行为</summary>
public sealed class AuthenticationBoundaryOptionsTests
{
    /// <summary>验证 Validate_RejectsMissingIssuer 场景</summary>
    [Fact]
    public void Validate_RejectsMissingIssuer()
    {
        var options = new AuthenticationBoundaryOptions(
            ValidIssuer: "",
            ValidAudience: "billing-api",
            RequiredScopes: ["billing.read"]);

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT issuer must be configured");
    }
}
