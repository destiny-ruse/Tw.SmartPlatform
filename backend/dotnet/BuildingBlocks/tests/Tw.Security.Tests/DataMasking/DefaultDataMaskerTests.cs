using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

public sealed class DefaultDataMaskerTests
{
    [Fact]
    public void Mask_Phone_HidesMiddleDigits()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("13800138000", SensitiveDataKind.PhoneNumber);

        masked.Should().Be("138****8000");
    }

    [Fact]
    public void Mask_Token_DoesNotExposeRawValue()
    {
        var masker = DefaultDataMasker.CreateDefault();

        var masked = masker.Mask("token-abcdef", SensitiveDataKind.Token);

        masked.Should().Be("***");
    }
}
