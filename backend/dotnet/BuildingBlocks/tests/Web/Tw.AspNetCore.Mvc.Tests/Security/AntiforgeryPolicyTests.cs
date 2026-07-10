using AwesomeAssertions;
using Tw.AspNetCore.Mvc.Security;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.Security;

/// <summary>
/// 覆盖防伪策略的核心行为和边界条件
/// </summary>
public sealed class AntiforgeryPolicyTests
{
    /// <summary>
    /// 验证要求Validation返回false针对Bearer读取请求
    /// </summary>
    [Fact]
    public void RequiresValidation_ReturnsFalse_ForBearerGetRequest()
    {
        AntiforgeryPolicy.RequiresValidation("GET", "Bearer")
            .Should()
            .BeFalse();
    }

    /// <summary>
    /// 验证要求Validation返回true针对CookiePOST请求
    /// </summary>
    [Fact]
    public void RequiresValidation_ReturnsTrue_ForCookiePostRequest()
    {
        AntiforgeryPolicy.RequiresValidation("POST", "Cookies")
            .Should()
            .BeTrue();
    }
}
