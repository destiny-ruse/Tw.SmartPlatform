using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Exceptions;
using Tw.Localization.Tests.Fakes;
using Xunit;

namespace Tw.Localization.Tests;

public class LocalizationServiceCollectionExtensionsTests
{
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

    [Fact]
    public void AddLocalization_Throws_WhenOptionsInvalid()
    {
        var services = new ServiceCollection();
        var act = () => services.AddLocalization(o => { o.DefaultCulture = "en-US"; });
        act.Should().Throw<TwConfigurationException>();
    }

    [Fact]
    public void AddLocalization_DoesNotOverrideBusinessEntityStore()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEntityTranslationStore, InMemoryEntityTranslationStore>();
        services.AddLocalization(o => { o.DefaultCulture = "en-US"; o.SupportedCultures.Add("en-US"); });
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEntityTranslationStore>().Should().BeOfType<InMemoryEntityTranslationStore>();
    }

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
