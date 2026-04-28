using FluentAssertions;
using Tw.Core.Context;
using Tw.Core.Timing;
using Xunit;

namespace Tw.Core.Tests;

public class ContextAndTimingTests
{
    [Fact]
    public void CurrentUser_Does_Not_Expose_TenantId()
    {
        typeof(ICurrentUser).GetProperty("TenantId").Should().BeNull();
    }

    [Fact]
    public void Clock_Interface_Exposes_Normalization_Contract()
    {
        typeof(IClock).GetMethod(nameof(IClock.Normalize)).Should().NotBeNull();
    }
}
