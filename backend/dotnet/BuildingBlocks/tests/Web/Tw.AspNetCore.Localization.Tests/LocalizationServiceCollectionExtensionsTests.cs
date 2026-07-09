using System;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>验证 LocalizationServiceCollectionExtensionsTests 相关行为</summary>
public class LocalizationServiceCollectionExtensionsTests
{
    /// <summary>验证 AddLocalization_RegistersWebAndCoreServices 场景</summary>
    [Fact]
    public void AddLocalization_RegistersWebAndCoreServices()
    {
        var services = new ServiceCollection();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<ITextLocalizer>().Should().NotBeNull();
        sp.GetRequiredService<ICurrentLocalizationContextAccessor>().Should().BeOfType<CurrentLocalizationContextAccessor>();
        sp.GetRequiredService<IStringLocalizerFactory>().Should().BeOfType<TwStringLocalizerFactory>();
        sp.GetRequiredService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>()
          .Should().BeOfType<TwStringLocalizer<LocalizationServiceCollectionExtensionsTests>>();
        sp.GetRequiredService<ICancellationTokenProvider>().Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

    /// <summary>验证 AddLocalization_ReturnsSameServiceCollection 场景</summary>
    [Fact]
    public void AddLocalization_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        result.Should().BeSameAs(services);
    }

    /// <summary>验证 AddLocalization_DoesNotOverrideExistingStringLocalizerFactory 场景</summary>
    [Fact]
    public void AddLocalization_DoesNotOverrideExistingStringLocalizerFactory()
    {
        var services = new ServiceCollection();
        services.AddScoped<IStringLocalizerFactory, FakeStringLocalizerFactory>();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IStringLocalizerFactory>()
            .Should().BeOfType<FakeStringLocalizerFactory>();
    }

    /// <summary>验证 FakeStringLocalizerFactory 相关行为</summary>
    private sealed class FakeStringLocalizerFactory : IStringLocalizerFactory
    {
        /// <summary>验证 Create 场景</summary>
        /// <param name="resourceSource">resourceSource 参数</param>
        /// <returns>Create 的执行结果</returns>
        public IStringLocalizer Create(Type resourceSource) => throw new NotSupportedException();
        /// <summary>验证 Create 场景</summary>
        /// <param name="baseName">baseName 参数</param>
        /// <param name="location">location 参数</param>
        /// <returns>Create 的执行结果</returns>
        public IStringLocalizer Create(string baseName, string location) => throw new NotSupportedException();
    }
}
