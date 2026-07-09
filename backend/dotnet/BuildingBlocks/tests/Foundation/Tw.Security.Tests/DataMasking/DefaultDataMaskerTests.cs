using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

/// <summary>验证 DefaultDataMaskerTests 相关行为</summary>
public sealed class DefaultDataMaskerTests
{
    /// <summary>验证 Mask_Phone_HidesMiddleDigits 场景</summary>
    [Fact]
    public void Mask_Phone_HidesMiddleDigits()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("13800138000", SensitiveDataKind.PhoneNumber);

        masked.Should().Be("138****8000");
    }

    /// <summary>验证 Mask_Token_DoesNotExposeRawValue 场景</summary>
    [Fact]
    public void Mask_Token_DoesNotExposeRawValue()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("token-abcdef", SensitiveDataKind.Token);

        masked.Should().Be("***");
    }
}
