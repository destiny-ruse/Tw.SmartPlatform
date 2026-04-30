using FluentAssertions;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Registration;
using Xunit;

namespace Tw.DependencyInjection.Tests.Interception;

/// <summary>
/// 验证 Service 与 Entry 拦截器链的顺序、作用域隔离和忽略规则
/// </summary>
public sealed class InterceptorChainTests
{
    [Fact]
    public void ServiceChain_Orders_Global_Matcher_Then_Explicit_Interceptors()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IInterceptedService, InterceptedService>()],
            [
                new InterceptorRegistration(typeof(LateGlobalInterceptor), InterceptorScope.Service, order: 20),
                new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service, order: -10),
            ],
            [
                new ServiceMatcher(typeof(LateMatchedInterceptor), order: 30),
                new ServiceMatcher(typeof(EarlyMatchedInterceptor), order: -20),
            ]);

        var chain = cache.GetInterceptors(typeof(InterceptedService));

        chain.Select(i => i.InterceptorType).Should().Equal(
            typeof(EarlyGlobalInterceptor),
            typeof(LateGlobalInterceptor),
            typeof(EarlyMatchedInterceptor),
            typeof(LateMatchedInterceptor),
            typeof(ExplicitInterceptor));
    }

    [Fact]
    public void ServiceChain_Sorts_Within_Layer_By_Order_Then_Interceptor_FullName()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IBasicService, BasicService>()],
            [
                new InterceptorRegistration(typeof(ZNamedInterceptor), InterceptorScope.Service),
                new InterceptorRegistration(typeof(ANamedInterceptor), InterceptorScope.Service),
                new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service, order: -1),
            ],
            []);

        var chain = cache.GetInterceptors(typeof(BasicService));

        chain.Select(i => i.InterceptorType).Should().Equal(
            typeof(EarlyGlobalInterceptor),
            typeof(ANamedInterceptor),
            typeof(ZNamedInterceptor));
    }

    [Fact]
    public void EntryChain_Orders_Global_Then_Matcher_Interceptors()
    {
        var cache = new EntryChainCache(
            [
                new InterceptorRegistration(typeof(LateGlobalInterceptor), InterceptorScope.Entry, order: 20),
                new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Entry, order: -10),
            ],
            [
                new EntryMatcher(typeof(LateMatchedInterceptor), order: 30),
                new EntryMatcher(typeof(EarlyMatchedInterceptor), order: -20),
            ]);

        var chain = cache.GetInterceptors(typeof(BasicController));

        chain.Select(i => i.InterceptorType).Should().Equal(
            typeof(EarlyGlobalInterceptor),
            typeof(LateGlobalInterceptor),
            typeof(EarlyMatchedInterceptor),
            typeof(LateMatchedInterceptor));
    }

    [Fact]
    public void ServiceScope_Matcher_Does_Not_Enter_Entry_Chain()
    {
        var cache = new EntryChainCache(
            [],
            [new ServiceMatcher(typeof(EarlyMatchedInterceptor))]);

        cache.GetInterceptors(typeof(BasicController)).Should().BeEmpty();
    }

    [Fact]
    public void EntryScope_Matcher_Does_Not_Enter_Service_Chain()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IBasicService, BasicService>()],
            [],
            [new EntryMatcher(typeof(EarlyMatchedInterceptor))]);

        cache.GetInterceptors(typeof(BasicService)).Should().BeEmpty();
    }

    [Fact]
    public void IgnoreInterceptors_Without_Arguments_Suppresses_Global_And_Matcher_But_Not_Explicit()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IIgnoreAllService, IgnoreAllService>()],
            [new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service)],
            [new ServiceMatcher(typeof(EarlyMatchedInterceptor))]);

        var chain = cache.GetInterceptors(typeof(IgnoreAllService));

        chain.Select(i => i.InterceptorType).Should().Equal(typeof(ExplicitInterceptor));
    }

    [Fact]
    public void IgnoreInterceptors_With_Type_Suppresses_Only_Matching_Global_And_Matcher_Interceptors()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<ISpecificIgnoreService, SpecificIgnoreService>()],
            [
                new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service),
                new InterceptorRegistration(typeof(LateGlobalInterceptor), InterceptorScope.Service),
            ],
            [
                new ServiceMatcher(typeof(EarlyMatchedInterceptor)),
                new ServiceMatcher(typeof(LateMatchedInterceptor)),
            ]);

        var chain = cache.GetInterceptors(typeof(SpecificIgnoreService));

        chain.Select(i => i.InterceptorType).Should().Equal(
            typeof(LateGlobalInterceptor),
            typeof(LateMatchedInterceptor));
    }

    [Fact]
    public void IgnoreInterceptors_On_Interface_Applies_To_Implementing_Class()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IInheritedIgnoredService, InheritedIgnoredService>()],
            [new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service)],
            [new ServiceMatcher(typeof(EarlyMatchedInterceptor))]);

        cache.GetInterceptors(typeof(InheritedIgnoredService)).Should().BeEmpty();
    }

    [Fact]
    public void Intercept_On_Interface_Applies_To_Implementing_Class()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IInterfaceInterceptedService, InterfaceInterceptedService>()],
            [],
            []);

        var chain = cache.GetInterceptors(typeof(InterfaceInterceptedService));

        chain.Select(i => i.InterceptorType).Should().Equal(typeof(ExplicitInterceptor));
    }

    [Fact]
    public void Cache_Returns_ReadOnly_Interceptor_Chains()
    {
        var cache = new ServiceChainCache(
            [Descriptor.For<IBasicService, BasicService>()],
            [new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Service)],
            []);

        var chain = cache.GetInterceptors(typeof(BasicService));
        var mutableView = (ICollection<InterceptorRegistration>)chain;

        var act = () => mutableView.Add(new InterceptorRegistration(typeof(LateGlobalInterceptor), InterceptorScope.Service));

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void EntryChain_Applies_IgnoreInterceptors_On_Entry_Type()
    {
        var cache = new EntryChainCache(
            [
                new InterceptorRegistration(typeof(EarlyGlobalInterceptor), InterceptorScope.Entry),
                new InterceptorRegistration(typeof(LateGlobalInterceptor), InterceptorScope.Entry),
            ],
            [new EntryMatcher(typeof(EarlyMatchedInterceptor))]);

        var chain = cache.GetInterceptors(typeof(IgnoreEntryController));

        chain.Select(i => i.InterceptorType).Should().Equal(
            typeof(LateGlobalInterceptor),
            typeof(EarlyMatchedInterceptor));
    }

    private interface IBasicService { }

    private sealed class BasicService : IBasicService { }

    private interface IInterceptedService { }

    [Intercept(typeof(ExplicitInterceptor))]
    private sealed class InterceptedService : IInterceptedService { }

    private interface IIgnoreAllService { }

    [IgnoreInterceptors]
    [Intercept(typeof(ExplicitInterceptor))]
    private sealed class IgnoreAllService : IIgnoreAllService { }

    private interface ISpecificIgnoreService { }

    [IgnoreInterceptors(typeof(EarlyGlobalInterceptor), typeof(EarlyMatchedInterceptor))]
    private sealed class SpecificIgnoreService : ISpecificIgnoreService { }

    [IgnoreInterceptors]
    private interface IBaseIgnoredService { }

    private interface IInheritedIgnoredService : IBaseIgnoredService { }

    private sealed class InheritedIgnoredService : IInheritedIgnoredService { }

    [Intercept(typeof(ExplicitInterceptor))]
    private interface IInterfaceInterceptedService { }

    private sealed class InterfaceInterceptedService : IInterfaceInterceptedService { }

    private sealed class BasicController { }

    [IgnoreInterceptors(typeof(EarlyGlobalInterceptor))]
    private sealed class IgnoreEntryController { }

    private sealed class EarlyGlobalInterceptor : InterceptorBase { }

    private sealed class LateGlobalInterceptor : InterceptorBase { }

    private sealed class EarlyMatchedInterceptor : InterceptorBase { }

    private sealed class LateMatchedInterceptor : InterceptorBase { }

    private sealed class ExplicitInterceptor : InterceptorBase { }

    private sealed class ANamedInterceptor : InterceptorBase { }

    private sealed class ZNamedInterceptor : InterceptorBase { }

    private sealed class ServiceMatcher(Type interceptorType, int order = 0) : IInterceptorMatcher
    {
        public Type InterceptorType { get; } = interceptorType;

        public InterceptorScope Scope => InterceptorScope.Service;

        public int Order { get; } = order;

        public bool MatchService(Type serviceType, Type implementationType) => true;
    }

    private sealed class EntryMatcher(Type interceptorType, int order = 0) : IInterceptorMatcher
    {
        public Type InterceptorType { get; } = interceptorType;

        public InterceptorScope Scope => InterceptorScope.Entry;

        public int Order { get; } = order;

        public bool MatchEntry(Type entryType) => true;
    }

    private static class Descriptor
    {
        public static ServiceRegistrationDescriptor For<TService, TImplementation>()
            => new(
                typeof(TImplementation),
                typeof(TService),
                Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
                Key: null,
                IsCollection: false,
                IsReplacement: false,
                Order: 0,
                AssemblyTopologicalIndex: 0,
                IsOpenGenericDefinition: false,
                AssemblyName: typeof(TImplementation).Assembly.GetName().Name ?? string.Empty);
    }
}
