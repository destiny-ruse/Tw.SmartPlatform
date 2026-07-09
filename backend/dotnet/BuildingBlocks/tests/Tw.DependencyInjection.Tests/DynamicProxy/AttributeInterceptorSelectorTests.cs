using System.Reflection;
using AwesomeAssertions;
using Tw.DependencyInjection.DynamicProxy;
using Tw.DynamicProxy.Abstractions;
using Xunit;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class AttributeInterceptorSelectorTests
{
    [Fact]
    public void SelectInterceptors_ReadsInterceptAttributesFromServiceImplementationAndMethod()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<ICompositeService>();

        var interceptors = selector.SelectInterceptors(typeof(CompositeService), typeof(ICompositeService), method);

        interceptors.Should().BeEquivalentTo([
            typeof(InterfaceInterceptor),
            typeof(ClassInterceptor),
            typeof(MethodInterceptor),
        ]);
    }

    [Fact]
    public void SelectInterceptors_ReturnsEmpty_WhenMethodDisablesInterception()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IMethodDisabledService>();

        var interceptors = selector.SelectInterceptors(
            typeof(MethodDisabledService),
            typeof(IMethodDisabledService),
            method);

        interceptors.Should().BeEmpty();
    }

    [Fact]
    public void SelectInterceptors_ReturnsEmpty_WhenClassDisablesInterception()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IClassDisabledService>();

        var interceptors = selector.SelectInterceptors(
            typeof(ClassDisabledService),
            typeof(IClassDisabledService),
            method);

        interceptors.Should().BeEmpty();
    }

    [Fact]
    public void SelectInterceptors_DeduplicatesInterceptorTypes()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IDuplicateService>();

        var interceptors = selector.SelectInterceptors(typeof(DuplicateService), typeof(IDuplicateService), method);

        interceptors.Should().Equal(typeof(DuplicateInterceptor));
    }

    [Fact]
    public void SelectInterceptors_OrdersByInterceptorOrderThenFullName()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IOrderedService>();

        var interceptors = selector.SelectInterceptors(typeof(OrderedService), typeof(IOrderedService), method);

        interceptors.Should().Equal(
            typeof(EarlyInterceptor),
            typeof(SameOrderAlphaInterceptor),
            typeof(SameOrderBetaInterceptor),
            typeof(LateInterceptor));
    }

    [Fact]
    public void SelectInterceptors_ReadsInterfaceMethodAttribute_WhenImplementationMethodProvided()
    {
        var selector = new AttributeInterceptorSelector();
        var method = typeof(ImplementationMethodInputService)
            .GetMethod(nameof(ImplementationMethodInputService.Execute), BindingFlags.Public | BindingFlags.Instance)!;

        var interceptors = selector.SelectInterceptors(
            typeof(ImplementationMethodInputService),
            typeof(IImplementationMethodInputService),
            method);

        interceptors.Should().BeEquivalentTo([
            typeof(ImplementationInputInterfaceMethodInterceptor),
            typeof(ImplementationInputMethodInterceptor),
        ]);
    }

    [Fact]
    public void SelectInterceptors_ReadsExplicitInterfaceAndImplementationAttributes_WhenImplementationMethodProvided()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetImplementationMethod<ExplicitService, IExplicitService>();

        var interceptors = selector.SelectInterceptors(typeof(ExplicitService), typeof(IExplicitService), method);

        interceptors.Should().BeEquivalentTo([
            typeof(ExplicitInterfaceMethodInterceptor),
            typeof(ExplicitImplementationMethodInterceptor),
        ]);
    }

    [Fact]
    public void SelectInterceptors_UsesInterfaceMap_WhenMultipleInterfacesShareSignature()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<ISecondSharedSignatureService>();

        var interceptors = selector.SelectInterceptors(
            typeof(SharedSignatureService),
            typeof(ISecondSharedSignatureService),
            method);

        interceptors.Should().BeEquivalentTo([
            typeof(SecondSharedInterfaceMethodInterceptor),
            typeof(SecondSharedImplementationMethodInterceptor),
        ]);
        interceptors.Should().NotContain([
            typeof(FirstSharedInterfaceMethodInterceptor),
            typeof(FirstSharedImplementationMethodInterceptor),
        ]);
    }

    private static MethodInfo GetServiceMethod<TService>(string methodName = "Execute") =>
        typeof(TService).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;

    private static MethodInfo GetImplementationMethod<TImplementation, TService>(string methodName = "Execute")
    {
        var interfaceMethod = GetServiceMethod<TService>(methodName);
        var interfaceMap = typeof(TImplementation).GetInterfaceMap(typeof(TService));
        var methodIndex = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);

        return interfaceMap.TargetMethods[methodIndex];
    }

    [Intercept(typeof(InterfaceInterceptor))]
    private interface ICompositeService
    {
        void Execute();
    }

    [Intercept(typeof(ClassInterceptor))]
    private sealed class CompositeService : ICompositeService
    {
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    [Intercept(typeof(InterfaceInterceptor))]
    private interface IMethodDisabledService
    {
        void Execute();
    }

    [Intercept(typeof(ClassInterceptor))]
    private sealed class MethodDisabledService : IMethodDisabledService
    {
        [DisableInterception]
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    [Intercept(typeof(InterfaceInterceptor))]
    private interface IClassDisabledService
    {
        void Execute();
    }

    [DisableInterception]
    [Intercept(typeof(ClassInterceptor))]
    private sealed class ClassDisabledService : IClassDisabledService
    {
        public void Execute()
        {
        }
    }

    [Intercept(typeof(DuplicateInterceptor))]
    private interface IDuplicateService
    {
        void Execute();
    }

    [Intercept(typeof(DuplicateInterceptor))]
    private sealed class DuplicateService : IDuplicateService
    {
        [Intercept(typeof(DuplicateInterceptor))]
        public void Execute()
        {
        }
    }

    [Intercept(typeof(SameOrderBetaInterceptor))]
    [Intercept(typeof(LateInterceptor))]
    [Intercept(typeof(EarlyInterceptor))]
    [Intercept(typeof(SameOrderAlphaInterceptor))]
    private interface IOrderedService
    {
        void Execute();
    }

    private sealed class OrderedService : IOrderedService
    {
        public void Execute()
        {
        }
    }

    private interface IImplementationMethodInputService
    {
        [Intercept(typeof(ImplementationInputInterfaceMethodInterceptor))]
        void Execute();
    }

    private sealed class ImplementationMethodInputService : IImplementationMethodInputService
    {
        [Intercept(typeof(ImplementationInputMethodInterceptor))]
        public void Execute()
        {
        }
    }

    private interface IExplicitService
    {
        [Intercept(typeof(ExplicitInterfaceMethodInterceptor))]
        void Execute();
    }

    private sealed class ExplicitService : IExplicitService
    {
        [Intercept(typeof(ExplicitImplementationMethodInterceptor))]
        void IExplicitService.Execute()
        {
        }
    }

    private interface IFirstSharedSignatureService
    {
        [Intercept(typeof(FirstSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    private interface ISecondSharedSignatureService
    {
        [Intercept(typeof(SecondSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    private sealed class SharedSignatureService : IFirstSharedSignatureService, ISecondSharedSignatureService
    {
        [Intercept(typeof(FirstSharedImplementationMethodInterceptor))]
        void IFirstSharedSignatureService.Execute(string value)
        {
        }

        [Intercept(typeof(SecondSharedImplementationMethodInterceptor))]
        void ISecondSharedSignatureService.Execute(string value)
        {
        }
    }

    private sealed class InterfaceInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class ClassInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class MethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class DuplicateInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    [InterceptorOrder(-10)]
    private sealed class EarlyInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    [InterceptorOrder(20)]
    private sealed class LateInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class SameOrderAlphaInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class SameOrderBetaInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class ImplementationInputInterfaceMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class ImplementationInputMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class ExplicitInterfaceMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class ExplicitImplementationMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class FirstSharedInterfaceMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class FirstSharedImplementationMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class SecondSharedInterfaceMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    private sealed class SecondSharedImplementationMethodInterceptor : IInterceptor
    {
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }
}
