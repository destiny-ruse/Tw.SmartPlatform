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
    public void ConfigurationSectionAttribute_Targets_Classes_And_Allows_Multiple_Declarations()
    {
        var usage = typeof(ConfigurationSectionAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), inherit: false)
            .Should()
            .ContainSingle()
            .Subject
            .Should()
            .BeOfType<AttributeUsageAttribute>()
            .Subject;

        usage.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeTrue();
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void ConfigurationSectionAttribute_Stores_Options_Metadata()
    {
        var attribute = new ConfigurationSectionAttribute("Auth")
        {
            OptionsName = "Primary",
            ValidateOnStart = true,
            DirectInject = true
        };

        attribute.Name.Should().Be("Auth");
        attribute.OptionsName.Should().Be("Primary");
        attribute.ValidateOnStart.Should().BeTrue();
        attribute.DirectInject.Should().BeTrue();
    }

    [Fact]
    public void ConfigurableOptions_Is_Marker_Interface()
    {
        typeof(IConfigurableOptions).GetMembers().Should().BeEmpty();
    }

    [Fact]
    public void ConfigurationSectionAttribute_ValidateOnStart_Defaults_To_True()
    {
        var attribute = new ConfigurationSectionAttribute("Auth");

        attribute.ValidateOnStart.Should().BeTrue();
    }

    [ConfigurationSection("ProbeFalse", ValidateOnStart = false)]
    private sealed class ProbeFalseFixture { }

    [ConfigurationSection("ProbeDefault")]
    private sealed class ProbeDefaultFixture { }

    [Fact]
    public void ConfigurationSectionAttribute_Accepts_Bool_Literal_In_Attribute_Syntax()
    {
        var explicitFalse = typeof(ProbeFalseFixture)
            .GetCustomAttributes(typeof(ConfigurationSectionAttribute), inherit: false)
            .Cast<ConfigurationSectionAttribute>()
            .Single();
        var defaulted = typeof(ProbeDefaultFixture)
            .GetCustomAttributes(typeof(ConfigurationSectionAttribute), inherit: false)
            .Cast<ConfigurationSectionAttribute>()
            .Single();

        explicitFalse.ValidateOnStart.Should().BeFalse();
        defaulted.ValidateOnStart.Should().BeTrue();
    }
}
