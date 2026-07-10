using System.Reflection;
using AwesomeAssertions;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;

namespace Tw.Castle.Core.Tests;

/// <summary>
/// 覆盖特性拦截器Selector的核心行为和边界条件
/// </summary>
public class AttributeInterceptorSelectorTests
{
    /// <summary>
    /// 验证Select拦截器集合ReadsInterceptAttributesFrom服务实现和方法
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合返回空当方法DisablesInterception
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合返回空当ClassDisablesInterception
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合Deduplicates拦截器类型集合
    /// </summary>
    [Fact]
    public void SelectInterceptors_DeduplicatesInterceptorTypes()
    {
        var selector = new AttributeInterceptorSelector();
        var method = GetServiceMethod<IDuplicateService>();

        var interceptors = selector.SelectInterceptors(typeof(DuplicateService), typeof(IDuplicateService), method);

        interceptors.Should().Equal(typeof(DuplicateInterceptor));
    }

    /// <summary>
    /// 验证Select拦截器集合OrdersBy拦截器OrderThenFull名称
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合ReadsInterface方法特性当实现方法Provided
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合ReadsExplicitInterface和实现Attributes当实现方法Provided
    /// </summary>
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

    /// <summary>
    /// 验证Select拦截器集合UsesInterface映射当MultipleInterfacesShareSignature
    /// </summary>
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

    /// <summary>
    /// 说明读取服务方法在当前类型中的职责
    /// </summary>
    /// <typeparam name="TService">响应数据的运行时类型</typeparam>
    /// <param name="methodName">用于提供方法Name</param>
    /// <returns>方法计算得到的文本值</returns>
    private static MethodInfo GetServiceMethod<TService>(string methodName = "Execute") =>
        typeof(TService).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;

    /// <summary>
    /// 说明读取实现方法在当前类型中的职责
    /// </summary>
    /// <typeparam name="TImplementation">响应数据的运行时类型</typeparam>
    /// <typeparam name="TService">响应数据的运行时类型</typeparam>
    /// <param name="methodName">用于提供方法Name</param>
    /// <returns>方法计算得到的文本值</returns>
    private static MethodInfo GetImplementationMethod<TImplementation, TService>(string methodName = "Execute")
    {
        var interfaceMethod = GetServiceMethod<TService>(methodName);
        var interfaceMap = typeof(TImplementation).GetInterfaceMap(typeof(TService));
        var methodIndex = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);

        return interfaceMap.TargetMethods[methodIndex];
    }

    /// <summary>
    /// 定义Composite服务的能力边界
    /// </summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface ICompositeService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 覆盖Composite服务的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(ClassInterceptor))]
    private sealed class CompositeService : ICompositeService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义方法Disabled服务的能力边界
    /// </summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface IMethodDisabledService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 覆盖MethodDisabled服务的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(ClassInterceptor))]
    private sealed class MethodDisabledService : IMethodDisabledService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [DisableInterception]
        [Intercept(typeof(MethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义ClassDisabled服务的能力边界
    /// </summary>
    [Intercept(typeof(InterfaceInterceptor))]
    private interface IClassDisabledService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 覆盖ClassDisabled服务的核心行为和边界条件
    /// </summary>
    [DisableInterception]
    [Intercept(typeof(ClassInterceptor))]
    private sealed class ClassDisabledService : IClassDisabledService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义重复服务的能力边界
    /// </summary>
    [Intercept(typeof(DuplicateInterceptor))]
    private interface IDuplicateService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 覆盖重复服务的核心行为和边界条件
    /// </summary>
    [Intercept(typeof(DuplicateInterceptor))]
    private sealed class DuplicateService : IDuplicateService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(DuplicateInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义Ordered服务的能力边界
    /// </summary>
    [Intercept(typeof(SameOrderBetaInterceptor))]
    [Intercept(typeof(LateInterceptor))]
    [Intercept(typeof(EarlyInterceptor))]
    [Intercept(typeof(SameOrderAlphaInterceptor))]
    private interface IOrderedService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// 覆盖Ordered服务的核心行为和边界条件
    /// </summary>
    private sealed class OrderedService : IOrderedService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义实现方法Input服务的能力边界
    /// </summary>
    private interface IImplementationMethodInputService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(ImplementationInputInterfaceMethodInterceptor))]
        void Execute();
    }

    /// <summary>
    /// 覆盖ImplementationMethodInput服务的核心行为和边界条件
    /// </summary>
    private sealed class ImplementationMethodInputService : IImplementationMethodInputService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(ImplementationInputMethodInterceptor))]
        public void Execute()
        {
        }
    }

    /// <summary>
    /// 定义Explicit服务的能力边界
    /// </summary>
    private interface IExplicitService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(ExplicitInterfaceMethodInterceptor))]
        void Execute();
    }

    /// <summary>
    /// 覆盖Explicit服务的核心行为和边界条件
    /// </summary>
    private sealed class ExplicitService : IExplicitService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        [Intercept(typeof(ExplicitImplementationMethodInterceptor))]
        void IExplicitService.Execute()
        {
        }
    }

    /// <summary>
    /// 定义第一个SharedSignature服务的能力边界
    /// </summary>
    private interface IFirstSharedSignatureService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        [Intercept(typeof(FirstSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    /// <summary>
    /// 定义第二个SharedSignature服务的能力边界
    /// </summary>
    private interface ISecondSharedSignatureService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        [Intercept(typeof(SecondSharedInterfaceMethodInterceptor))]
        void Execute(string value);
    }

    /// <summary>
    /// 覆盖SharedSignature服务的核心行为和边界条件
    /// </summary>
    private sealed class SharedSignatureService : IFirstSharedSignatureService, ISecondSharedSignatureService
    {
        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        [Intercept(typeof(FirstSharedImplementationMethodInterceptor))]
        void IFirstSharedSignatureService.Execute(string value)
        {
        }

        /// <summary>
        /// 说明Execute在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        [Intercept(typeof(SecondSharedImplementationMethodInterceptor))]
        void ISecondSharedSignatureService.Execute(string value)
        {
        }
    }

    /// <summary>
    /// 覆盖Interface拦截器的核心行为和边界条件
    /// </summary>
    private sealed class InterfaceInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖Class拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ClassInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖Method拦截器的核心行为和边界条件
    /// </summary>
    private sealed class MethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖重复拦截器的核心行为和边界条件
    /// </summary>
    private sealed class DuplicateInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖Early拦截器的核心行为和边界条件
    /// </summary>
    [InterceptorOrder(-10)]
    private sealed class EarlyInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖Late拦截器的核心行为和边界条件
    /// </summary>
    [InterceptorOrder(20)]
    private sealed class LateInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖SameOrderAlpha拦截器的核心行为和边界条件
    /// </summary>
    private sealed class SameOrderAlphaInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖SameOrderBeta拦截器的核心行为和边界条件
    /// </summary>
    private sealed class SameOrderBetaInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖ImplementationInputInterfaceMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ImplementationInputInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖ImplementationInputMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ImplementationInputMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖ExplicitInterfaceMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ExplicitInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖ExplicitImplementationMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ExplicitImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖FirstSharedInterfaceMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class FirstSharedInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖FirstSharedImplementationMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class FirstSharedImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖SecondSharedInterfaceMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class SecondSharedInterfaceMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }

    /// <summary>
    /// 覆盖SecondSharedImplementationMethod拦截器的核心行为和边界条件
    /// </summary>
    private sealed class SecondSharedImplementationMethodInterceptor : IInterceptor
    {
        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InterceptAsync(IInvocationContext context) => context.ProceedAsync();
    }
}
