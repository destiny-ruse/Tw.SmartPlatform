using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection.Autofac;
using Xunit;

namespace Tw.DependencyInjection.Autofac.Tests;

public sealed class AutofacHostBuilderExtensionsTests
{
    [Fact]
    public void UseAutofac_ReplacesServiceProvider_WithAutofac()
    {
        var builder = new HostBuilder().UseAutofac();

        using var host = builder.Build();

        host.Services.Should().BeOfType<AutofacServiceProvider>();
    }

    [Fact]
    public void UseAutofac_Throws_WhenHostBuilderIsNull()
    {
        IHostBuilder hostBuilder = null!;

        var act = () => hostBuilder.UseAutofac();

        act.Should().Throw<ArgumentNullException>();
    }
}
