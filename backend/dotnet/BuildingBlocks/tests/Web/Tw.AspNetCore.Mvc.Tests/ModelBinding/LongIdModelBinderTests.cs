using AwesomeAssertions;
using Tw.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ModelBinding;

/// <summary>
/// 覆盖长整型标识模型绑定器的核心行为和边界条件
/// </summary>
public sealed class LongIdModelBinderTests
{
    /// <summary>
    /// 验证TryParse返回false当值Exceeds长整型
    /// </summary>
    [Fact]
    public void TryParse_ReturnsFalse_WhenValueExceedsLong()
    {
        LongIdModelBinder.TryParse("999999999999999999999999", out _)
            .Should()
            .BeFalse();
    }
}
