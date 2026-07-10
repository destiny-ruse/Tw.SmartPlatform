using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Configuration;
using Tw.DependencyInjection.Tests.Fixtures;
using Xunit;

namespace Tw.DependencyInjection.Tests.Configuration;

/// <summary>
/// 覆盖选项绑定Executor的核心行为和边界条件
/// </summary>
public class OptionsBindingExecutorTests
{
    /// <summary>
    /// 验证ApplyBindsValidates和RunsPOSTConfigure
    /// </summary>
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

    /// <summary>
    /// 验证Apply抛出异常服务Registration异常当Section缺少
    /// </summary>
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

    /// <summary>
    /// 验证Apply注册ExplicitValidator
    /// </summary>
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

    /// <summary>
    /// 验证Apply注册校验OnStart针对DataAnnotations
    /// </summary>
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

    /// <summary>
    /// 覆盖RejectingIntegration缓存选项Validator的核心行为和边界条件
    /// </summary>
    public sealed class RejectingIntegrationCacheOptionsValidator : IValidateOptions<IntegrationCacheOptions>
    {
        /// <summary>
        /// 校验当前配置或输入约束，并在非法时抛出异常
        /// </summary>
        /// <param name="name">待匹配成员或资源的名称</param>
        /// <param name="options">用于配置当前组件行为的选项</param>
        /// <returns>方法计算得到的文本值</returns>
        public ValidateOptionsResult Validate(string? name, IntegrationCacheOptions options)
        {
            return ValidateOptionsResult.Fail("显式校验失败");
        }
    }
}
