using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>验证 DependencyLifetimeResolverTests 相关行为</summary>
public class DependencyLifetimeResolverTests
{
    /// <summary>验证 ScopedService 相关行为</summary>
    private sealed class ScopedService : IScopedDependency;
    /// <summary>验证 MultiLifetimeService 相关行为</summary>
    private sealed class MultiLifetimeService : IScopedDependency, ISingletonDependency;

    /// <summary>验证 AttributeLifetimeService 相关行为</summary>
    [ServiceRegistration(DependencyLifetime.Singleton)]
    private sealed class AttributeLifetimeService : IScopedDependency;

    /// <summary>验证 NoLifetimeService 相关行为</summary>
    [ServiceRegistration]
    private sealed class NoLifetimeService;

    /// <summary>验证 CacheOptions 相关行为</summary>
    private sealed class CacheOptions : IConfigurableOptions;
    /// <summary>验证 AbstractService 相关行为</summary>
    private abstract class AbstractService : IScopedDependency;

    /// <summary>验证 PlainService 相关行为</summary>
    private sealed class PlainService;
    /// <summary>定义 IPlainContract 契约</summary>
    private interface IPlainContract;

    /// <summary>验证 DisabledService 相关行为</summary>
    [DisableServiceRegistration]
    private sealed class DisabledService : IScopedDependency;

    /// <summary>验证 ResolveLifetime_UsesMarkerInterface 场景</summary>
    [Fact]
    public void ResolveLifetime_UsesMarkerInterface()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(ScopedService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Scoped);
        reason.Should().BeNull();
    }

    /// <summary>验证 ResolveLifetime_AttributeOverridesMarker 场景</summary>
    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out _)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
    }

    /// <summary>验证 ResolveLifetime_FailsWhenMultipleMarkersDeclared 场景</summary>
    [Fact]
    public void ResolveLifetime_FailsWhenMultipleMarkersDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(MultiLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("多个生命周期标记");
    }

    /// <summary>验证 ShouldSkipOrdinaryRegistration_SkipsOptionsAndAbstractTypes 场景</summary>
    [Fact]
    public void ShouldSkipOrdinaryRegistration_SkipsOptionsAndAbstractTypes()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(CacheOptions), out var optionsReason)
            .Should().BeTrue();
        optionsReason.Should().Contain("Options");

        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(AbstractService), out var abstractReason)
            .Should().BeTrue();
        abstractReason.Should().Contain("抽象");
    }

    /// <summary>验证 ResolveLifetime_SkipsWhenNoLifetimeDeclared 场景</summary>
    [Fact]
    public void ResolveLifetime_SkipsWhenNoLifetimeDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(NoLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("未声明生命周期");
    }

    /// <summary>验证 Mapper_MapsToMicrosoftServiceLifetime 场景</summary>
    /// <param name="source">source 参数</param>
    /// <param name="expected">expected 参数</param>
    [Theory]
    [InlineData(DependencyLifetime.Transient, ServiceLifetime.Transient)]
    [InlineData(DependencyLifetime.Scoped, ServiceLifetime.Scoped)]
    [InlineData(DependencyLifetime.Singleton, ServiceLifetime.Singleton)]
    public void Mapper_MapsToMicrosoftServiceLifetime(DependencyLifetime source, ServiceLifetime expected)
    {
        DependencyLifetimeMapper.Map(source).Should().Be(expected);
    }

    // 3a. ResolveLifetime_AttributeOverridesMarker 补充 reason 断言
    /// <summary>验证 ResolveLifetime_AttributeOverridesMarker_ReasonIsNull 场景</summary>
    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker_ReasonIsNull()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
        reason.Should().BeNull();
    }

    // 3b. IsRegistrationParticipant 覆盖
    /// <summary>验证 IsRegistrationParticipant_ReturnsTrue_WhenMarkerInterface 场景</summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsTrue_WhenMarkerInterface()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(ScopedService))
            .Should().BeTrue();
    }

    /// <summary>验证 IsRegistrationParticipant_ReturnsTrue_WhenAttributeOnly 场景</summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsTrue_WhenAttributeOnly()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(NoLifetimeService))
            .Should().BeTrue();
    }

    /// <summary>验证 IsRegistrationParticipant_ReturnsFalse_WhenPlainType 场景</summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsFalse_WhenPlainType()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(PlainService))
            .Should().BeFalse();
    }

    // 3c. ShouldSkipOrdinaryRegistration 接口分支
    /// <summary>验证 ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenInterface 场景</summary>
    [Fact]
    public void ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenInterface()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(IPlainContract), out var reason)
            .Should().BeTrue();
        reason.Should().Contain("接口");
    }

    // 3d. ShouldSkipOrdinaryRegistration Disable 分支
    /// <summary>验证 ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenDisabled 场景</summary>
    [Fact]
    public void ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenDisabled()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(DisabledService), out var reason)
            .Should().BeTrue();
        reason.Should().Contain("DisableServiceRegistration");
    }

    // 3e. DependencyLifetimeMapper.Map 异常路径
    /// <summary>验证 Mapper_Map_Throws_WhenUnknownLifetime 场景</summary>
    [Fact]
    public void Mapper_Map_Throws_WhenUnknownLifetime()
    {
        var act = () => DependencyLifetimeMapper.Map((DependencyLifetime)99);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
