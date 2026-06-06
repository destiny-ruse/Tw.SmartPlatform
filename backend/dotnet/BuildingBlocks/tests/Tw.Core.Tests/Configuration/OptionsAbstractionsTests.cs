using FluentAssertions;
using Tw.Configuration.Abstractions;
using Xunit;

namespace Tw.Core.Tests.Configuration;

public class OptionsAbstractionsTests
{
    [Fact]
    public void IConfigurableOptions_LivesIn_AbstractionsNamespace()
    {
        typeof(IConfigurableOptions).Namespace.Should().Be("Tw.Configuration.Abstractions");
    }
}
