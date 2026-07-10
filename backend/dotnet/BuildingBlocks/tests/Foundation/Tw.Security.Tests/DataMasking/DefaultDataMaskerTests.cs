using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

/// <summary>
/// 覆盖默认DataMasker的核心行为和边界条件
/// </summary>
public sealed class DefaultDataMaskerTests
{
    /// <summary>
    /// 验证MaskPhoneHidesMiddleDigits
    /// </summary>
    [Fact]
    public void Mask_Phone_HidesMiddleDigits()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("13800138000", SensitiveDataKind.PhoneNumber);

        masked.Should().Be("138****8000");
    }

    /// <summary>
    /// 验证Mask令牌不ExposeRaw值
    /// </summary>
    [Fact]
    public void Mask_Token_DoesNotExposeRawValue()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("token-abcdef", SensitiveDataKind.Token);

        masked.Should().Be("***");
    }
}
