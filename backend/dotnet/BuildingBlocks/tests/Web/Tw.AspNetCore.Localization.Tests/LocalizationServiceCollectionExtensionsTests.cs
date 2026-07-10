using System;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

/// <summary>
/// 覆盖本地化服务CollectionExtensions的核心行为和边界条件
/// </summary>
public class LocalizationServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加本地化注册Web和CoreServices
    /// </summary>
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

    /// <summary>
    /// 验证添加本地化返回Same服务Collection
    /// </summary>
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

    /// <summary>
    /// 验证添加本地化不OverrideExistingStringLocalizerFactory
    /// </summary>
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

    /// <summary>
    /// 覆盖FakeStringLocalizerFactory的核心行为和边界条件
    /// </summary>
    private sealed class FakeStringLocalizerFactory : IStringLocalizerFactory
    {
        /// <summary>
        /// 创建统一 API 错误响应对象
        /// </summary>
        /// <param name="resourceSource">用于提供resourceSource</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public IStringLocalizer Create(Type resourceSource) => throw new NotSupportedException();
        /// <summary>
        /// 创建统一 API 错误响应对象
        /// </summary>
        /// <param name="baseName">用于提供基类Name</param>
        /// <param name="location">用于提供location</param>
        /// <returns>方法计算得到的文本值</returns>
        public IStringLocalizer Create(string baseName, string location) => throw new NotSupportedException();
    }
}
