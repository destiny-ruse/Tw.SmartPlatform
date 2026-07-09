using AwesomeAssertions;
using Tw.AspNetCore.Mvc.Security;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Security;

public sealed class AntiforgeryPolicyTests
{
    [Fact]
    public void RequiresValidation_ReturnsFalse_ForBearerGetRequest()
    {
        AntiforgeryPolicy.RequiresValidation("GET", "Bearer")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void RequiresValidation_ReturnsTrue_ForCookiePostRequest()
    {
        AntiforgeryPolicy.RequiresValidation("POST", "Cookies")
            .Should()
            .BeTrue();
    }
}
