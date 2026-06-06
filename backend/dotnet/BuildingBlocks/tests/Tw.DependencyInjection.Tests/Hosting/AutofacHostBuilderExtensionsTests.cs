using Autofac.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection;
using Xunit;

namespace Tw.DependencyInjection.Tests.Hosting;

public class AutofacHostBuilderExtensionsTests
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
