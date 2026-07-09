using System;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Tw.AspNetCore.Mvc.Context;
using Tw.Threading;
using Tw.Localization;
using Xunit;

namespace Tw.AspNetCore.Localization.Tests;

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
        sp.GetRequiredService<IStringLocalizer<LocalizationServiceCollectionExtensionsTests>>()
          .Should().BeOfType<TwStringLocalizer<LocalizationServiceCollectionExtensionsTests>>();
        sp.GetRequiredService<ICancellationTokenProvider>().Should().BeOfType<HttpContextCancellationTokenProvider>();
    }

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

    private sealed class FakeStringLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource) => throw new NotSupportedException();
        public IStringLocalizer Create(string baseName, string location) => throw new NotSupportedException();
    }
}
