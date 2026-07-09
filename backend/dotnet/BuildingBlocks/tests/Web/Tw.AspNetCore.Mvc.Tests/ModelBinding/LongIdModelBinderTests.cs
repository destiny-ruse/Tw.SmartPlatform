using AwesomeAssertions;
using Tw.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ModelBinding;

/// <summary>验证 LongIdModelBinderTests 相关行为</summary>
public sealed class LongIdModelBinderTests
{
    /// <summary>验证 TryParse_ReturnsFalse_WhenValueExceedsLong 场景</summary>
    [Fact]
    public void TryParse_ReturnsFalse_WhenValueExceedsLong()
    {
        LongIdModelBinder.TryParse("999999999999999999999999", out _)
            .Should()
            .BeFalse();
    }
}
