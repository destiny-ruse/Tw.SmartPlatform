using AwesomeAssertions;
using Tw.DependencyInjection;
using Tw.DependencyInjection.Discovery;
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

    [Fact]
    public void Filter_KeepsTwPrefix_ByDefault()
    {
        var result = AssemblyFilter.Filter(
            ["Tw.Core", "Tw.Order.Application", "System.Text.Json", "Newtonsoft.Json"],
            new ServiceRegistrationOptions());

        result.Should().BeEquivalentTo("Tw.Core", "Tw.Order.Application");
    }

    [Fact]
    public void Filter_IncludesExplicitAssembly_WithoutTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Acme.Payments");

        var result = AssemblyFilter.Filter(["Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Acme.Payments");
    }

    [Fact]
    public void Filter_IncludesCustomPrefix_InAdditionToTw()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblyPrefixes.Add("Acme.");

        var result = AssemblyFilter.Filter(["Tw.Core", "Acme.Payments", "Contoso.Crm"], options);

        result.Should().BeEquivalentTo("Tw.Core", "Acme.Payments");
    }

    [Fact]
    public void Filter_ExcludesByName_EvenWhenTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblies.Add("Tw.Legacy");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.Legacy"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    [Fact]
    public void Filter_ExcludesByPrefix_EvenWhenTwPrefix()
    {
        var options = new ServiceRegistrationOptions();
        options.ExcludeAssemblyPrefixes.Add("Tw.Test");

        var result = AssemblyFilter.Filter(["Tw.Core", "Tw.TestKit"], options);

        result.Should().BeEquivalentTo("Tw.Core");
    }

    [Fact]
    public void Filter_BlacklistWins_WhenNameBothIncludedAndExcluded()
    {
        var options = new ServiceRegistrationOptions();
        options.IncludeAssemblies.Add("Tw.Order");
        options.ExcludeAssemblies.Add("Tw.Order");

        var result = AssemblyFilter.Filter(["Tw.Order"], options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Options_DefaultsAssemblyPrioritiesToEmptyDictionary()
    {
        var options = new ServiceRegistrationOptions();

        options.AssemblyPriorities.Should().BeEmpty();
    }
}
