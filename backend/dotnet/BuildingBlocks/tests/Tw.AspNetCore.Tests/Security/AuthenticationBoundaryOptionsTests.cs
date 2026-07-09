using AwesomeAssertions;
using Tw.AspNetCore.Security;
using Xunit;

namespace Tw.AspNetCore.Tests.Security;

public sealed class AuthenticationBoundaryOptionsTests
{
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
