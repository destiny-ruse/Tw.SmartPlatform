using System.Reflection;
using FluentAssertions;
using Tw.DependencyInjection.Abstractions;
using Xunit;

namespace Tw.Core.Tests.DependencyInjection;

public class RegistrationAttributeTests
{
    [ServiceRegistration(DependencyLifetime.Scoped, Priority = 10)]
    private sealed class WithLifetime;

    [ServiceRegistration]
    private sealed class WithoutLifetime;

    [ExposeServices(typeof(IContract), IncludeSelf = true)]
    private sealed class Exposing;

    [ExposeKeyedService(typeof(IContract), "wechat")]
    private sealed class Keyed;

    private interface IContract;

    [Fact]
    public void ServiceRegistration_HasNoReplaceMember()
    {
        typeof(ServiceRegistrationAttribute).GetProperty("Replace").Should().BeNull();
        typeof(ServiceRegistrationAttribute).GetField("Replace").Should().BeNull();
    }

    [Fact]
    public void ServiceRegistration_CarriesLifetimeAndPriority()
    {
        var attr = typeof(WithLifetime).GetCustomAttribute<ServiceRegistrationAttribute>()!;
        attr.Lifetime.Should().Be(DependencyLifetime.Scoped);
        attr.Priority.Should().Be(10);
    }

    [Fact]
    public void ServiceRegistration_LifetimeIsNull_WhenNotSpecified()
    {
        var attr = typeof(WithoutLifetime).GetCustomAttribute<ServiceRegistrationAttribute>()!;
        attr.Lifetime.Should().BeNull();
    }

    [Fact]
    public void ExposeServices_CarriesTypesAndIncludeSelf()
    {
        var attr = typeof(Exposing).GetCustomAttribute<ExposeServicesAttribute>()!;
        attr.ServiceTypes.Should().ContainSingle().Which.Should().Be(typeof(IContract));
        attr.IncludeSelf.Should().BeTrue();
    }

    [Fact]
    public void ExposeKeyedService_CarriesContractAndKey()
    {
        var attr = typeof(Keyed).GetCustomAttribute<ExposeKeyedServiceAttribute>()!;
        attr.ServiceType.Should().Be(typeof(IContract));
        attr.Key.Should().Be("wechat");
    }

    [Fact]
    public void TwAssemblyPriority_TargetsAssembly()
    {
        var usage = typeof(TwAssemblyPriorityAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.ValidOn.Should().Be(AttributeTargets.Assembly);
    }

    [Fact]
    public void ExposeServices_AllowsMultiple()
    {
        var usage = typeof(ExposeServicesAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        usage.AllowMultiple.Should().BeTrue();
    }
}
