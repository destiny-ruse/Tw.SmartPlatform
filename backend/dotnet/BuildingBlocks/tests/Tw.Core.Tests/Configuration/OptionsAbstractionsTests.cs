using System.Reflection;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Tw.Configuration.Abstractions;
using Xunit;

namespace Tw.Core.Tests.Configuration;

public class OptionsAbstractionsTests
{
    private sealed class CacheOptions : IConfigurableOptions<CacheOptions>
    {
        public int Ttl { get; set; }

        public void PostConfigure(CacheOptions options, IConfiguration configuration)
        {
            if (options.Ttl == 0)
            {
                options.Ttl = 60;
            }
        }
    }

    private sealed class CacheOptionsValidator;

    [Fact]
    public void IConfigurableOptions_LivesIn_AbstractionsNamespace()
    {
        typeof(IConfigurableOptions).Namespace.Should().Be("Tw.Configuration.Abstractions");
    }

    [Fact]
    public void GenericOptions_Implements_NonGenericMarker()
    {
        typeof(CacheOptions).Should().BeAssignableTo<IConfigurableOptions>();
    }

    [Fact]
    public void PostConfigure_FillsDefault_WhenUnset()
    {
        var options = new CacheOptions();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        ((IConfigurableOptions<CacheOptions>)options).PostConfigure(options, configuration);

        options.Ttl.Should().Be(60);
    }

    [Fact]
    public void OptionsSection_CarriesPath()
    {
        var attr = new OptionsSectionAttribute("Tw:Cache");
        attr.Path.Should().Be("Tw:Cache");
    }

    [Fact]
    public void OptionsName_CarriesName()
    {
        new OptionsNameAttribute("primary").Name.Should().Be("primary");
    }

    [Fact]
    public void SensitiveConfiguration_TargetsClassAndProperty()
    {
        var usage = typeof(SensitiveConfigurationAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Class | AttributeTargets.Property);
    }

    [Fact]
    public void OptionsValidator_CarriesValidatorType()
    {
        var attr = new OptionsValidatorAttribute(typeof(CacheOptionsValidator));
        attr.ValidatorType.Should().Be(typeof(CacheOptionsValidator));
    }
}
