using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Abstractions;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Registration;

/// <summary>
/// 覆盖依赖LifetimeResolver的核心行为和边界条件
/// </summary>
public class DependencyLifetimeResolverTests
{
    /// <summary>
    /// 覆盖Scoped服务的核心行为和边界条件
    /// </summary>
    private sealed class ScopedService : IScopedDependency;
    /// <summary>
    /// 覆盖MultiLifetime服务的核心行为和边界条件
    /// </summary>
    private sealed class MultiLifetimeService : IScopedDependency, ISingletonDependency;

    /// <summary>
    /// 覆盖特性Lifetime服务的核心行为和边界条件
    /// </summary>
    [ServiceRegistration(DependencyLifetime.Singleton)]
    private sealed class AttributeLifetimeService : IScopedDependency;

    /// <summary>
    /// 覆盖NoLifetime服务的核心行为和边界条件
    /// </summary>
    [ServiceRegistration]
    private sealed class NoLifetimeService;

    /// <summary>
    /// 覆盖缓存选项的核心行为和边界条件
    /// </summary>
    private sealed class CacheOptions : IConfigurableOptions;
    /// <summary>
    /// 覆盖Abstract服务的核心行为和边界条件
    /// </summary>
    private abstract class AbstractService : IScopedDependency;

    /// <summary>
    /// 覆盖Plain服务的核心行为和边界条件
    /// </summary>
    private sealed class PlainService;
    /// <summary>
    /// 定义PlainContract的能力边界
    /// </summary>
    private interface IPlainContract;

    /// <summary>
    /// 覆盖Disabled服务的核心行为和边界条件
    /// </summary>
    [DisableServiceRegistration]
    private sealed class DisabledService : IScopedDependency;

    /// <summary>
    /// 验证ResolveLifetimeUsesMarkerInterface
    /// </summary>
    [Fact]
    public void ResolveLifetime_UsesMarkerInterface()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(ScopedService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Scoped);
        reason.Should().BeNull();
    }

    /// <summary>
    /// 验证ResolveLifetime特性OverridesMarker
    /// </summary>
    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out _)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
    }

    /// <summary>
    /// 验证ResolveLifetimeFails当MultipleMarkersDeclared
    /// </summary>
    [Fact]
    public void ResolveLifetime_FailsWhenMultipleMarkersDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(MultiLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("多个生命周期标记");
    }

    /// <summary>
    /// 验证ShouldSkipOrdinaryRegistrationSkips选项和Abstract类型集合
    /// </summary>
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

    /// <summary>
    /// 验证ResolveLifetimeSkips当NoLifetimeDeclared
    /// </summary>
    [Fact]
    public void ResolveLifetime_SkipsWhenNoLifetimeDeclared()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(NoLifetimeService), out _, out var reason)
            .Should().BeFalse();
        reason.Should().Contain("未声明生命周期");
    }

    /// <summary>
    /// 验证Mapper映射到Microsoft服务Lifetime
    /// </summary>
    /// <param name="source">用于提供source</param>
    /// <param name="expected">用于提供expected</param>
    [Theory]
    [InlineData(DependencyLifetime.Transient, ServiceLifetime.Transient)]
    [InlineData(DependencyLifetime.Scoped, ServiceLifetime.Scoped)]
    [InlineData(DependencyLifetime.Singleton, ServiceLifetime.Singleton)]
    public void Mapper_MapsToMicrosoftServiceLifetime(DependencyLifetime source, ServiceLifetime expected)
    {
        DependencyLifetimeMapper.Map(source).Should().Be(expected);
    }

    // 3a. ResolveLifetime_AttributeOverridesMarker 补充 reason 断言
    /// <summary>
    /// 验证ResolveLifetime特性OverridesMarkerReasonIs空值
    /// </summary>
    [Fact]
    public void ResolveLifetime_AttributeOverridesMarker_ReasonIsNull()
    {
        ServiceTypeInspector.TryResolveLifetime(typeof(AttributeLifetimeService), out var lifetime, out var reason)
            .Should().BeTrue();
        lifetime.Should().Be(DependencyLifetime.Singleton);
        reason.Should().BeNull();
    }

    // 3b. IsRegistrationParticipant 覆盖
    /// <summary>
    /// 验证sRegistrationParticipant返回true当MarkerInterface
    /// </summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsTrue_WhenMarkerInterface()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(ScopedService))
            .Should().BeTrue();
    }

    /// <summary>
    /// 验证sRegistrationParticipant返回true当特性Only
    /// </summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsTrue_WhenAttributeOnly()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(NoLifetimeService))
            .Should().BeTrue();
    }

    /// <summary>
    /// 验证sRegistrationParticipant返回false当Plain类型
    /// </summary>
    [Fact]
    public void IsRegistrationParticipant_ReturnsFalse_WhenPlainType()
    {
        ServiceTypeInspector.IsRegistrationParticipant(typeof(PlainService))
            .Should().BeFalse();
    }

    // 3c. ShouldSkipOrdinaryRegistration 接口分支
    /// <summary>
    /// 验证ShouldSkipOrdinaryRegistration返回true当Interface
    /// </summary>
    [Fact]
    public void ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenInterface()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(IPlainContract), out var reason)
            .Should().BeTrue();
        reason.Should().Contain("接口");
    }

    // 3d. ShouldSkipOrdinaryRegistration Disable 分支
    /// <summary>
    /// 验证ShouldSkipOrdinaryRegistration返回true当Disabled
    /// </summary>
    [Fact]
    public void ShouldSkipOrdinaryRegistration_ReturnsTrue_WhenDisabled()
    {
        ServiceTypeInspector.ShouldSkipOrdinaryRegistration(typeof(DisabledService), out var reason)
            .Should().BeTrue();
        reason.Should().Contain("DisableServiceRegistration");
    }

    // 3e. DependencyLifetimeMapper.Map 异常路径
    /// <summary>
    /// 验证Mapper映射抛出异常当UnknownLifetime
    /// </summary>
    [Fact]
    public void Mapper_Map_Throws_WhenUnknownLifetime()
    {
        var act = () => DependencyLifetimeMapper.Map((DependencyLifetime)99);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
