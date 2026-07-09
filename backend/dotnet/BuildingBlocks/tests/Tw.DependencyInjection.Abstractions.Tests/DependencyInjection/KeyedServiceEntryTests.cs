using AwesomeAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Abstractions.Tests.DependencyInjection;

public class KeyedServiceEntryTests
{
    private interface IProvider;
    private sealed class Provider : IProvider;

    [Fact]
    public void Entry_CarriesKeyAndService()
    {
        IProvider provider = new Provider();

        var entry = new KeyedServiceEntry<IProvider>("wechat", provider);

        entry.Key.Should().Be("wechat");
        entry.Service.Should().BeSameAs(provider);
    }

    [Fact]
    public void Entry_LivesIn_AbstractionsNamespace()
    {
        typeof(KeyedServiceEntry<IProvider>).Namespace.Should().Be("Tw.DependencyInjection.Abstractions");
    }
}
