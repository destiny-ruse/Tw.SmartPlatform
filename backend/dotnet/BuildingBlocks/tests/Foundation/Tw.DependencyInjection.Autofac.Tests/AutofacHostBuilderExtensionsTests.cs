using Autofac.Extensions.DependencyInjection;
using AwesomeAssertions;
using Microsoft.Extensions.Hosting;
using Tw.DependencyInjection.Autofac;
using Xunit;

namespace Tw.DependencyInjection.Autofac.Tests;

/// <summary>验证 AutofacHostBuilderExtensionsTests 相关行为</summary>
public sealed class AutofacHostBuilderExtensionsTests
{
    /// <summary>验证 UseAutofac_ReplacesServiceProvider_WithAutofac 场景</summary>
    [Fact]
    public void UseAutofac_ReplacesServiceProvider_WithAutofac()
    {
        var builder = new HostBuilder().UseAutofac();

        using var host = builder.Build();

        host.Services.Should().BeOfType<AutofacServiceProvider>();
    }

    /// <summary>验证 UseAutofac_Throws_WhenHostBuilderIsNull 场景</summary>
    [Fact]
    public void UseAutofac_Throws_WhenHostBuilderIsNull()
    {
        IHostBuilder hostBuilder = null!;

        var act = () => hostBuilder.UseAutofac();

        act.Should().Throw<ArgumentNullException>();
    }
}
