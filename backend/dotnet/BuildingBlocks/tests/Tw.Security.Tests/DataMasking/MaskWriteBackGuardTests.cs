using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

public sealed class MaskWriteBackGuardTests
{
    [Fact]
    public void EnsureNotMaskedValue_RejectsMaskedPhoneWriteBack()
    {
        var guard = new MaskWriteBackGuard(DefaultDataMasker.CreateDefault());

        var act = () => guard.EnsureNotMaskedValue("138****8000", SensitiveDataKind.PhoneNumber);

        act.Should().Throw<MaskedValueWriteBackException>()
            .WithMessage("不能把脱敏值写回敏感字段");
    }
}
