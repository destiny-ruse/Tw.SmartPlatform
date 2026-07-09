using AwesomeAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Abstractions.Tests;

public sealed class DependencyLifetimeTests
{
    [Fact]
    public void DependencyLifetime_ContainsExpectedValues()
    {
        Enum.GetNames<DependencyLifetime>()
            .Should()
            .BeEquivalentTo("Transient", "Scoped", "Singleton");
    }
}
