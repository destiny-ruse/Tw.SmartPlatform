using AwesomeAssertions;
using Tw.Security.DataMasking;
using Xunit;

namespace Tw.Security.Tests.DataMasking;

/// <summary>
/// 覆盖MaskWriteBackGuard的核心行为和边界条件
/// </summary>
public sealed class MaskWriteBackGuardTests
{
    /// <summary>
    /// 验证Ensure不Masked值拒绝MaskedPhoneWrite回
    /// </summary>
    [Fact]
    public void EnsureNotMaskedValue_RejectsMaskedPhoneWriteBack()
    {
        var guard = new MaskWriteBackGuard(DefaultDataMasker.CreateDefault());

        var act = () => guard.EnsureNotMaskedValue("138****8000", SensitiveDataKind.PhoneNumber);

        act.Should().Throw<MaskedValueWriteBackException>()
            .WithMessage("不能把脱敏值写回敏感字段");
    }
}
