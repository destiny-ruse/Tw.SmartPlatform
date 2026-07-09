using AwesomeAssertions;
using Tw.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Tw.AspNetCore.Mvc.Tests.ModelBinding;

public sealed class LongIdModelBinderTests
{
    [Fact]
    public void TryParse_ReturnsFalse_WhenValueExceedsLong()
    {
        LongIdModelBinder.TryParse("999999999999999999999999", out _)
            .Should()
            .BeFalse();
    }
}
