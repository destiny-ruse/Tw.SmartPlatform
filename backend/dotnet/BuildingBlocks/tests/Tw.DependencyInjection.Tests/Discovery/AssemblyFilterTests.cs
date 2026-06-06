using FluentAssertions;
using Tw.DependencyInjection;
using Xunit;

namespace Tw.DependencyInjection.Tests.Discovery;

public class AssemblyFilterTests
{
    [Fact]
    public void Options_DefaultsToEmptyLists()
    {
        var options = new ServiceRegistrationOptions();

        options.IncludeAssemblies.Should().BeEmpty();
        options.ExcludeAssemblies.Should().BeEmpty();
        options.IncludeAssemblyPrefixes.Should().BeEmpty();
        options.ExcludeAssemblyPrefixes.Should().BeEmpty();
    }
}
