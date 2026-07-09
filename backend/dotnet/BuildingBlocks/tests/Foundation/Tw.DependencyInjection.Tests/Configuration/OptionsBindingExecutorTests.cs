using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

/// <summary>验证 OptionsBindingExecutorTests 相关行为</summary>
public class OptionsBindingExecutorTests
{
    /// <summary>验证 Apply_BindsValidatesAndRunsPostConfigure 场景</summary>
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

    /// <summary>验证 Apply_ThrowsServiceRegistrationException_WhenSectionMissing 场景</summary>
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

    /// <summary>验证 Apply_RegistersExplicitValidator 场景</summary>
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

    /// <summary>验证 Apply_RegistersValidateOnStartForDataAnnotations 场景</summary>
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

    /// <summary>验证 RejectingIntegrationCacheOptionsValidator 相关行为</summary>
    public sealed class RejectingIntegrationCacheOptionsValidator : IValidateOptions<IntegrationCacheOptions>
    {
        /// <summary>验证 Validate 场景</summary>
        /// <param name="name">name 参数</param>
        /// <param name="options">options 参数</param>
        /// <returns>Validate 的执行结果</returns>
        public ValidateOptionsResult Validate(string? name, IntegrationCacheOptions options)
        {
            return ValidateOptionsResult.Fail("显式校验失败");
        }
    }
}
