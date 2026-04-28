using FluentAssertions;
using Tw.Core.Configuration;
using Xunit;

namespace Tw.Core.Tests;

public class ConfigurationTests
{
    [Fact]
    public void ConfigurationSectionAttribute_Stores_Name()
    {
        var attribute = new ConfigurationSectionAttribute("Auth");

        attribute.Name.Should().Be("Auth");
    }

    [Fact]
    public void ConfigurableOptions_Is_Marker_Interface()
    {
        typeof(IConfigurableOptions).GetMembers().Should().BeEmpty();
    }
}
