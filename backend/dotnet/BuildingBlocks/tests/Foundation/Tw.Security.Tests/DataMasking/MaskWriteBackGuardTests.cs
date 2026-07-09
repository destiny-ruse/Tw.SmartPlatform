using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

/// <summary>验证 MaskWriteBackGuardTests 相关行为</summary>
public sealed class MaskWriteBackGuardTests
{
    /// <summary>验证 EnsureNotMaskedValue_RejectsMaskedPhoneWriteBack 场景</summary>
    [Fact]
    public void EnsureNotMaskedValue_RejectsMaskedPhoneWriteBack()
    {
        var guard = new MaskWriteBackGuard(DefaultDataMasker.CreateDefault());

        var act = () => guard.EnsureNotMaskedValue("138****8000", SensitiveDataKind.PhoneNumber);

        act.Should().Throw<MaskedValueWriteBackException>()
            .WithMessage("不能把脱敏值写回敏感字段");
    }
}
