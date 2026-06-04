using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
}
