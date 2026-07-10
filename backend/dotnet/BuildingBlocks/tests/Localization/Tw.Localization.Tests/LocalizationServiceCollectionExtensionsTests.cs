using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Exceptions;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖本地化服务CollectionExtensions的核心行为和边界条件
/// </summary>
public class LocalizationServiceCollectionExtensionsTests
{
    /// <summary>
    /// 验证添加本地化注册CoreServices
    /// </summary>
    [Fact]
    public void AddLocalization_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
        });

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITextLocalizer>().Should().BeOfType<TextLocalizer>();
        provider.GetRequiredService<IEntityTranslationService>().Should().BeOfType<EntityTranslationService>();
        provider.GetRequiredService<IStaticTextSnapshot>().Should().NotBeNull();
    }

    /// <summary>
    /// 验证添加本地化抛出异常当选项非法
    /// </summary>
    [Fact]
    public void AddLocalization_Throws_WhenOptionsInvalid()
    {
        var services = new ServiceCollection();
        var act = () => services.AddLocalization(o => { o.DefaultCulture = "en-US"; });
        act.Should().Throw<TwConfigurationException>();
    }

    /// <summary>
    /// 验证添加本地化不OverrideBusinessEntity存储
    /// </summary>
    [Fact]
    public void AddLocalization_DoesNotOverrideBusinessEntityStore()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEntityTranslationStore, InMemoryEntityTranslationStore>();
        services.AddLocalization(o => { o.DefaultCulture = "en-US"; o.SupportedCultures.Add("en-US"); });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEntityTranslationStore>().Should().BeOfType<InMemoryEntityTranslationStore>();
    }

    /// <summary>
    /// 验证添加本地化抛出异常当JSON路径缺少
    /// </summary>
    [Fact]
    public void AddLocalization_Throws_WhenJsonPathMissing()
    {
        var services = new ServiceCollection();
        var act = () => services.AddLocalization(o =>
        {
            o.DefaultCulture = "en-US";
            o.SupportedCultures.Add("en-US");
            o.JsonResourcePaths.Add("does-not-exist.app.json");
        });
        act.Should().Throw<TwConfigurationException>();
    }
}
