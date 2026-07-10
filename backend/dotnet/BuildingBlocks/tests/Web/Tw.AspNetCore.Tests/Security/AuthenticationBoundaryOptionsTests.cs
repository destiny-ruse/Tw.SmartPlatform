using AwesomeAssertions;
using Tw.AspNetCore.Security;
using Xunit;

namespace Tw.AspNetCore.Tests.Security;

/// <summary>
/// 覆盖认证边界选项的核心行为和边界条件
/// </summary>
public sealed class AuthenticationBoundaryOptionsTests
{
    /// <summary>
    /// 验证校验拒绝缺少签发方
    /// </summary>
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
