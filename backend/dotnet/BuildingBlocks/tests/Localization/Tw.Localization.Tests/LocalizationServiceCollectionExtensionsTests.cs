using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Exceptions;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>验证 LocalizationServiceCollectionExtensionsTests 相关行为</summary>
public class LocalizationServiceCollectionExtensionsTests
{
    /// <summary>验证 AddLocalization_RegistersCoreServices 场景</summary>
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

    /// <summary>验证 AddLocalization_Throws_WhenOptionsInvalid 场景</summary>
    [Fact]
    public void AddLocalization_Throws_WhenOptionsInvalid()
    {
        var services = new ServiceCollection();
        var act = () => services.AddLocalization(o => { o.DefaultCulture = "en-US"; });
        act.Should().Throw<TwConfigurationException>();
    }

    /// <summary>验证 AddLocalization_DoesNotOverrideBusinessEntityStore 场景</summary>
    [Fact]
    public void AddLocalization_DoesNotOverrideBusinessEntityStore()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEntityTranslationStore, InMemoryEntityTranslationStore>();
        services.AddLocalization(o => { o.DefaultCulture = "en-US"; o.SupportedCultures.Add("en-US"); });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEntityTranslationStore>().Should().BeOfType<InMemoryEntityTranslationStore>();
    }

    /// <summary>验证 AddLocalization_Throws_WhenJsonPathMissing 场景</summary>
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
