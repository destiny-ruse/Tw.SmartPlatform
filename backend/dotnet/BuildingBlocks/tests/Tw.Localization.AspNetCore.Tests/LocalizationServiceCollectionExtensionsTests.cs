using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Context;
using Tw.Context;
using Xunit;

namespace Tw.Localization.AspNetCore.Tests;

public class LocalizationServiceCollectionExtensionsTests
{
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
        sp.GetRequiredService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>().Should().NotBeNull();
        sp.GetRequiredService<ICancellationTokenProvider>().Should().BeOfType<HttpContextCancellationTokenProvider>();
    }
}
