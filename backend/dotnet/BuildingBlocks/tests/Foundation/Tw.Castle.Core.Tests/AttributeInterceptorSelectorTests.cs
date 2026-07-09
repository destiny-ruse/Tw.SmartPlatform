using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>验证 AttributeInterceptorSelectorTests 相关行为</summary>
public class AttributeInterceptorSelectorTests
{
    /// <summary>验证 SelectInterceptors_ReadsInterceptAttributesFromServiceImplementationAndMethod 场景</summary>
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

    /// <summary>验证 SelectInterceptors_ReturnsEmpty_WhenMethodDisablesInterception 场景</summary>
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

    /// <summary>验证 SelectInterceptors_ReturnsEmpty_WhenClassDisablesInterception 场景</summary>
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

    /// <summary>验证 SelectInterceptors_DeduplicatesInterceptorTypes 场景</summary>
    [Fact]
    public void SelectInterceptors_DeduplicatesInterceptorTypes()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IDuplicateService>();

        var interceptors = selector.SelectInterceptors(typeof(DuplicateService), typeof(IDuplicateService), method);

        interceptors.Should().Equal(typeof(DuplicateInterceptor));
    }

    /// <summary>验证 SelectInterceptors_OrdersByInterceptorOrderThenFullName 场景</summary>
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

    /// <summary>验证 SelectInterceptors_ReadsInterfaceMethodAttribute_WhenImplementationMethodProvided 场景</summary>
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

    /// <summary>验证 SelectInterceptors_ReadsExplicitInterfaceAndImplementationAttributes_WhenImplementationMethodProvided 场景</summary>
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

    /// <summary>验证 SelectInterceptors_UsesInterfaceMap_WhenMultipleInterfacesShareSignature 场景</summary>
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

    /// <summary>验证 GetServiceMethod 场景</summary>
    /// <typeparam name="TService">TService 类型参数</typeparam>
    /// <param name="methodName">methodName 参数</param>
    /// <returns>GetServiceMethod 的执行结果</returns>
    private static MethodInfo GetServiceMethod<TService>(string methodName = "Execute") =>
        typeof(TService).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;

    /// <summary>验证 GetImplementationMethod 场景</summary>
    /// <typeparam name="TImplementation">TImplementation 类型参数</typeparam>
    /// <typeparam name="TService">TService 类型参数</typeparam>
    /// <param name="methodName">methodName 参数</param>
    /// <returns>GetImplementationMethod 的执行结果</returns>
    private static MethodInfo GetImplementationMethod<TImplementation, TService>(string methodName = "Execute")
    {
        var interfaceMethod = GetServiceMethod<TService>(methodName);
        var interfaceMap = typeof(TImplementation).GetInterfaceMap(typeof(TService));
        var methodIndex = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);

        return interfaceMap.TargetMethods[methodIndex];
    }

    /// <summary>定义 ICompositeService 契约</summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface ICompositeService
    {
        /// <summary>验证 Execute 场景</summary>
        void Execute();
    }

    /// <summary>验证 CompositeService 相关行为</summary>
    [Intercept(typeof(ClassInterceptor))]
    private sealed class CompositeService : ICompositeService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>定义 IMethodDisabledService 契约</summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface IMethodDisabledService
    {
        /// <summary>验证 Execute 场景</summary>
        void Execute();
    }

    /// <summary>验证 MethodDisabledService 相关行为</summary>
    [Intercept(typeof(ClassInterceptor))]
    private sealed class MethodDisabledService : IMethodDisabledService
    {
        /// <summary>验证 Execute 场景</summary>
        [DisableInterception]
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>定义 IClassDisabledService 契约</summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface IClassDisabledService
    {
        /// <summary>验证 Execute 场景</summary>
        void Execute();
    }

    /// <summary>验证 ClassDisabledService 相关行为</summary>
    [DisableInterception]
    [Intercept(typeof(ClassInterceptor))]
    private sealed class ClassDisabledService : IClassDisabledService
    {
        /// <summary>验证 Execute 场景</summary>
        public void Execute()
        {
        }
    }

    /// <summary>定义 IDuplicateService 契约</summary>
    [Intercept(typeof(DuplicateInterceptor))]
    private interface IDuplicateService
    {
        /// <summary>验证 Execute 场景</summary>
        void Execute();
    }

    /// <summary>验证 DuplicateService 相关行为</summary>
    [Intercept(typeof(DuplicateInterceptor))]
    private sealed class DuplicateService : IDuplicateService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(DuplicateInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>定义 IOrderedService 契约</summary>
    [Intercept(typeof(SameOrderBetaInterceptor))]
    [Intercept(typeof(LateInterceptor))]
    [Intercept(typeof(EarlyInterceptor))]
    [Intercept(typeof(SameOrderAlphaInterceptor))]
    private interface IOrderedService
    {
        /// <summary>验证 Execute 场景</summary>
        void Execute();
    }

    /// <summary>验证 OrderedService 相关行为</summary>
    private sealed class OrderedService : IOrderedService
    {
        /// <summary>验证 Execute 场景</summary>
        public void Execute()
        {
        }
    }

    /// <summary>定义 IImplementationMethodInputService 契约</summary>
    private interface IImplementationMethodInputService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(ImplementationInputInterfaceMethodInterceptor))]
        void Execute();
    }

    /// <summary>验证 ImplementationMethodInputService 相关行为</summary>
    private sealed class ImplementationMethodInputService : IImplementationMethodInputService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(ImplementationInputMethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>定义 IExplicitService 契约</summary>
    private interface IExplicitService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(ExplicitInterfaceMethodInterceptor))]
        void Execute();
    }

    /// <summary>验证 ExplicitService 相关行为</summary>
    private sealed class ExplicitService : IExplicitService
    {
        /// <summary>验证 Execute 场景</summary>
        [Intercept(typeof(ExplicitImplementationMethodInterceptor))]
        void IExplicitService.Execute()
        {
        }
    }

    /// <summary>定义 IFirstSharedSignatureService 契约</summary>
    private interface IFirstSharedSignatureService
    {
        /// <summary>验证 Execute 场景</summary>
        /// <param name="value">value 参数</param>
        [Intercept(typeof(FirstSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    /// <summary>定义 ISecondSharedSignatureService 契约</summary>
    private interface ISecondSharedSignatureService
    {
        /// <summary>验证 Execute 场景</summary>
        /// <param name="value">value 参数</param>
        [Intercept(typeof(SecondSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    /// <summary>验证 SharedSignatureService 相关行为</summary>
    private sealed class SharedSignatureService : IFirstSharedSignatureService, ISecondSharedSignatureService
    {
        /// <summary>验证 Execute 场景</summary>
        /// <param name="value">value 参数</param>
        [Intercept(typeof(FirstSharedImplementationMethodInterceptor))]
        void IFirstSharedSignatureService.Execute(string value)
        {
        }

        /// <summary>验证 Execute 场景</summary>
        /// <param name="value">value 参数</param>
        [Intercept(typeof(SecondSharedImplementationMethodInterceptor))]
        void ISecondSharedSignatureService.Execute(string value)
        {
        }
    }

    /// <summary>验证 InterfaceInterceptor 相关行为</summary>
    private sealed class InterfaceInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 ClassInterceptor 相关行为</summary>
    private sealed class ClassInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 MethodInterceptor 相关行为</summary>
    private sealed class MethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 DuplicateInterceptor 相关行为</summary>
    private sealed class DuplicateInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 EarlyInterceptor 相关行为</summary>
    [InterceptorOrder(-10)]
    private sealed class EarlyInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 LateInterceptor 相关行为</summary>
    [InterceptorOrder(20)]
    private sealed class LateInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 SameOrderAlphaInterceptor 相关行为</summary>
    private sealed class SameOrderAlphaInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 SameOrderBetaInterceptor 相关行为</summary>
    private sealed class SameOrderBetaInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 ImplementationInputInterfaceMethodInterceptor 相关行为</summary>
    private sealed class ImplementationInputInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 ImplementationInputMethodInterceptor 相关行为</summary>
    private sealed class ImplementationInputMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 ExplicitInterfaceMethodInterceptor 相关行为</summary>
    private sealed class ExplicitInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 ExplicitImplementationMethodInterceptor 相关行为</summary>
    private sealed class ExplicitImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 FirstSharedInterfaceMethodInterceptor 相关行为</summary>
    private sealed class FirstSharedInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 FirstSharedImplementationMethodInterceptor 相关行为</summary>
    private sealed class FirstSharedImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 SecondSharedInterfaceMethodInterceptor 相关行为</summary>
    private sealed class SecondSharedInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>验证 SecondSharedImplementationMethodInterceptor 相关行为</summary>
    private sealed class SecondSharedImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }
}
