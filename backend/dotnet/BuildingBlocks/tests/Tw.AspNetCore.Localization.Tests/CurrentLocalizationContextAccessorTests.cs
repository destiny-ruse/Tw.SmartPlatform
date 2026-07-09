using AwesomeAssertions;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

public class CurrentLocalizationContextAccessorTests
{
    [Fact]
    public void Current_DefaultsToNull()
    {
        new CurrentLocalizationContextAccessor().Current.Should().BeNull();
    }

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
