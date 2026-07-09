using AwesomeAssertions;
using Tw.AspNetCore.Mvc.Security;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Security;

/// <summary>验证 AntiforgeryPolicyTests 相关行为</summary>
public sealed class AntiforgeryPolicyTests
{
    /// <summary>验证 RequiresValidation_ReturnsFalse_ForBearerGetRequest 场景</summary>
    [Fact]
    public void RequiresValidation_ReturnsFalse_ForBearerGetRequest()
    {
        AntiforgeryPolicy.RequiresValidation("GET", "Bearer")
            .Should()
            .BeFalse();
    }

    /// <summary>验证 RequiresValidation_ReturnsTrue_ForCookiePostRequest 场景</summary>
    [Fact]
    public void RequiresValidation_ReturnsTrue_ForCookiePostRequest()
    {
        AntiforgeryPolicy.RequiresValidation("POST", "Cookies")
            .Should()
            .BeTrue();
    }
}
