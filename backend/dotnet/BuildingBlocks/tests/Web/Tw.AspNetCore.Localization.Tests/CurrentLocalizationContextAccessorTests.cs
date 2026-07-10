using AwesomeAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖Current本地化上下文Accessor的核心行为和边界条件
/// </summary>
public class CurrentLocalizationContextAccessorTests
{
    /// <summary>
    /// 验证CurrentDefaults到空值
    /// </summary>
    [Fact]
    public void Current_DefaultsToNull()
    {
        new CurrentLocalizationContextAccessor().Current.Should().BeNull();
    }

    /// <summary>
    /// 验证CurrentRoundTripsAssigned上下文
    /// </summary>
    [Fact]
    public void Current_RoundTripsAssignedContext()
    {
        var accessor = new CurrentLocalizationContextAccessor
        {
            Current = new LocalizationContext("zh-Hans") { TenantId = "tenant-a" },
        };

        accessor.Current!.CultureName.Should().Be("zh-Hans");
        accessor.Current!.TenantId.Should().Be("tenant-a");
    }
}
