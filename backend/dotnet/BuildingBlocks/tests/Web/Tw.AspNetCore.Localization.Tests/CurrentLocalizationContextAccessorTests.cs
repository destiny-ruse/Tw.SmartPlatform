using AwesomeAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>验证 CurrentLocalizationContextAccessorTests 相关行为</summary>
public class CurrentLocalizationContextAccessorTests
{
    /// <summary>验证 Current_DefaultsToNull 场景</summary>
    [Fact]
    public void Current_DefaultsToNull()
    {
        new CurrentLocalizationContextAccessor().Current.Should().BeNull();
    }

    /// <summary>验证 Current_RoundTripsAssignedContext 场景</summary>
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
