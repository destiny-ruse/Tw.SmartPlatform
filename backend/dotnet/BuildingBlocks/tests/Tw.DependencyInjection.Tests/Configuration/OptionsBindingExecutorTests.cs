using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

public class OptionsBindingExecutorTests
{
    [Fact]
    public void Apply_BindsValidatesAndRunsPostConfigure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
            })
            .Build();
        var services = new ServiceCollection();
        var candidate = new OptionsBindingCandidate(
            typeof(IntegrationCacheOptions),
            "IntegrationCache",
            Options.DefaultName,
            SectionExists: true,
            IsSensitive: false,
            ValidatorType: null);

        OptionsBindingExecutor.Apply(services, configuration, [candidate]);
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var options = provider.GetRequiredService<IOptions<IntegrationCacheOptions>>().Value;

        options.Endpoint.Should().Be("localhost");
        options.EffectiveEndpoint.Should().Be("localhost");
    }

    [Fact]
    public void Apply_ThrowsServiceRegistrationException_WhenSectionMissing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        var candidate = new OptionsBindingCandidate(
            typeof(IntegrationCacheOptions),
            "IntegrationCache",
            Options.DefaultName,
            SectionExists: false,
            IsSensitive: false,
            ValidatorType: null);

        var act = () => OptionsBindingExecutor.Apply(services, configuration, [candidate]);

        act.Should().Throw<ServiceRegistrationException>()
            .WithMessage("*必填配置节缺失*IntegrationCache*");
    }

    [Fact]
    public void Apply_RegistersExplicitValidator()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Endpoint"] = "localhost",
            })
            .Build();
        var services = new ServiceCollection();
        var candidate = new OptionsBindingCandidate(
            typeof(IntegrationCacheOptions),
            "IntegrationCache",
            Options.DefaultName,
            SectionExists: true,
            IsSensitive: false,
            ValidatorType: typeof(RejectingIntegrationCacheOptionsValidator));

        OptionsBindingExecutor.Apply(services, configuration, [candidate]);
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var act = () => provider.GetRequiredService<IOptions<IntegrationCacheOptions>>().Value;

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*显式校验失败*");
    }

    [Fact]
    public void Apply_RegistersValidateOnStartForDataAnnotations()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntegrationCache:Enabled"] = "true",
            })
            .Build();
        var services = new ServiceCollection();
        var candidate = new OptionsBindingCandidate(
            typeof(IntegrationCacheOptions),
            "IntegrationCache",
            Options.DefaultName,
            SectionExists: true,
            IsSensitive: false,
            ValidatorType: null);

        OptionsBindingExecutor.Apply(services, configuration, [candidate]);
        using var provider = services.BuildServiceProvider(validateScopes: true);

        var act = () => provider.GetRequiredService<IStartupValidator>().Validate();

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*DataAnnotation validation failed*Endpoint*");
    }

    public sealed class RejectingIntegrationCacheOptionsValidator : IValidateOptions<IntegrationCacheOptions>
    {
        public ValidateOptionsResult Validate(string? name, IntegrationCacheOptions options)
        {
            return ValidateOptionsResult.Fail("显式校验失败");
        }
    }
}
