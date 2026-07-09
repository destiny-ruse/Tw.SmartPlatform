using Castle.DynamicProxy;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.Castle.Core;
using Tw.Castle.Core.Abstractions;
using Xunit;
using AbstractionInterceptor = Tw.Castle.Core.Abstractions.IInterceptor;
using CastleInterceptor = Castle.DynamicProxy.IInterceptor;
using DependencyInterceptorSelector = Tw.Castle.Core.IInterceptorSelector;

namespace Tw.Castle.Core.Tests;

/// <summary>验证 CastleInvocationContextTests 相关行为</summary>
public class CastleInvocationContextTests
{
    /// <summary>验证 ProceedAsync_ProceedsTargetAndCapturesReturnValue_ForSyncTaskAndValueTaskMethods 场景</summary>
    /// <returns>ProceedAsync_ProceedsTargetAndCapturesReturnValue_ForSyncTaskAndValueTaskMethods 的执行结果</returns>
    [Fact]
    public async Task ProceedAsync_ProceedsTargetAndCapturesReturnValue_ForSyncTaskAndValueTaskMethods()
    {
        var target = new InvocationTarget();
        var interceptor = new ProceedAsyncRecordingInterceptor();
        var proxy = CreateProxy(target, interceptor);

        var syncResult = proxy.Sync("source");
        await proxy.TaskAsync("source");
        var taskResult = await proxy.TaskOfStringAsync("source");
        await proxy.ValueTaskAsync("source");
        var valueTaskResult = await proxy.ValueTaskOfStringAsync("source");

        syncResult.Should().Be("sync:rewritten");
        taskResult.Should().Be("task:rewritten");
        valueTaskResult.Should().Be("value-task:rewritten");
        target.ReceivedValues.Should().Equal("rewritten", "rewritten", "rewritten", "rewritten", "rewritten");
        interceptor.ReturnValues.Should().Equal([
            "sync:rewritten",
            null,
            "task:rewritten",
            null,
            "value-task:rewritten",
        ]);
    }

    /// <summary>验证 ProceedAsync_WritesArgumentChangesBackToCastleInvocation 场景</summary>
    [Fact]
    public void ProceedAsync_WritesArgumentChangesBackToCastleInvocation()
    {
        var target = new InvocationTarget();
        var interceptor = new ProceedAsyncRecordingInterceptor();
        var proxy = CreateProxy(target, interceptor);

        proxy.Sync("source");

        target.ReceivedValues.Should().Equal("rewritten");
        interceptor.InvocationArgumentValues.Should().Equal("rewritten");
    }

    /// <summary>验证 ReturnValueSetter_WritesModifiedValueBackToCastleInvocation 场景</summary>
    [Fact]
    public void ReturnValueSetter_WritesModifiedValueBackToCastleInvocation()
    {
        var target = new InvocationTarget();
        var interceptor = new ReturnValueOverrideInterceptor();
        var proxy = CreateProxy(target, interceptor);

        var result = proxy.Sync("source");

        result.Should().Be("intercepted");
    }

    /// <summary>验证 Proceed_ThrowsInvalidOperationException_WhenTargetMethodReturnsTask 场景</summary>
    [Fact]
    public void Proceed_ThrowsInvalidOperationException_WhenTargetMethodReturnsTask()
    {
        var target = new InvocationTarget();
        var interceptor = new SynchronousProceedInterceptor();
        var proxy = CreateProxy(target, interceptor);

        Action act = () => _ = proxy.TaskAsync("source");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*异步目标方法*");
    }

    /// <summary>验证 Proceed_ThrowsInvalidOperationException_WhenTargetMethodReturnsValueTask 场景</summary>
    [Fact]
    public void Proceed_ThrowsInvalidOperationException_WhenTargetMethodReturnsValueTask()
    {
        var target = new InvocationTarget();
        var interceptor = new SynchronousProceedInterceptor();
        var proxy = CreateProxy(target, interceptor);

        Action act = () => _ = proxy.ValueTaskAsync("source");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*异步目标方法*");
    }

    /// <summary>验证 CreateProxy 场景</summary>
    /// <param name="target">target 参数</param>
    /// <param name="interceptor">interceptor 参数</param>
    /// <returns>CreateProxy 的执行结果</returns>
    private static IInvocationTarget CreateProxy(InvocationTarget target, CastleInterceptor interceptor)
    {
        var generator = new ProxyGenerator();

        return generator.CreateInterfaceProxyWithTargetInterface<IInvocationTarget>(target, interceptor);
    }

    /// <summary>定义 IInvocationTarget 契约</summary>
    public interface IInvocationTarget
    {
        /// <summary>验证 Sync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Sync 的执行结果</returns>
        string Sync(string value);

        /// <summary>验证 TaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskAsync 的执行结果</returns>
        Task TaskAsync(string value);

        /// <summary>验证 TaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskOfStringAsync 的执行结果</returns>
        Task<string> TaskOfStringAsync(string value);

        /// <summary>验证 ValueTaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskAsync 的执行结果</returns>
        ValueTask ValueTaskAsync(string value);

        /// <summary>验证 ValueTaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskOfStringAsync 的执行结果</returns>
        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    /// <summary>验证 InvocationTarget 相关行为</summary>
    private sealed class InvocationTarget : IInvocationTarget
    {
        /// <summary>表示 ReceivedValues 属性</summary>
        public List<string> ReceivedValues { get; } = [];

        /// <summary>验证 Sync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Sync 的执行结果</returns>
        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        /// <summary>验证 TaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskAsync 的执行结果</returns>
        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        /// <summary>验证 TaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskOfStringAsync 的执行结果</returns>
        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        /// <summary>验证 ValueTaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskAsync 的执行结果</returns>
        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        /// <summary>验证 ValueTaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskOfStringAsync 的执行结果</returns>
        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    /// <summary>验证 ProceedAsyncRecordingInterceptor 相关行为</summary>
    private sealed class ProceedAsyncRecordingInterceptor : CastleInterceptor
    {
        /// <summary>表示 ReturnValues 属性</summary>
        public List<object?> ReturnValues { get; } = [];

        /// <summary>表示 InvocationArgumentValues 属性</summary>
        public List<object?> InvocationArgumentValues { get; } = [];

        /// <summary>验证 Intercept 场景</summary>
        /// <param name="invocation">invocation 参数</param>
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.Arguments[0] = "rewritten";
            context.ProceedAsync().AsTask().GetAwaiter().GetResult();

            ReturnValues.Add(context.ReturnValue);
            InvocationArgumentValues.Add(invocation.GetArgumentValue(0));
            invocation.ReturnValue = CreateCompatibleReturnValue(invocation.Method.ReturnType, context.ReturnValue);
        }
    }

    /// <summary>验证 ReturnValueOverrideInterceptor 相关行为</summary>
    private sealed class ReturnValueOverrideInterceptor : CastleInterceptor
    {
        /// <summary>验证 Intercept 场景</summary>
        /// <param name="invocation">invocation 参数</param>
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.ProceedAsync().AsTask().GetAwaiter().GetResult();
            context.ReturnValue = "intercepted";
        }
    }

    /// <summary>验证 SynchronousProceedInterceptor 相关行为</summary>
    private sealed class SynchronousProceedInterceptor : CastleInterceptor
    {
        /// <summary>验证 Intercept 场景</summary>
        /// <param name="invocation">invocation 参数</param>
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.Proceed();
        }
    }

    /// <summary>验证 CreateCompatibleReturnValue 场景</summary>
    /// <param name="returnType">returnType 参数</param>
    /// <param name="returnValue">returnValue 参数</param>
    /// <returns>CreateCompatibleReturnValue 的执行结果</returns>
    private static object? CreateCompatibleReturnValue(Type returnType, object? returnValue)
    {
        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType == typeof(Task<string>))
        {
            return Task.FromResult((string)returnValue!);
        }

        if (returnType == typeof(ValueTask))
        {
            return ValueTask.CompletedTask;
        }

        if (returnType == typeof(ValueTask<string>))
        {
            return ValueTask.FromResult((string)returnValue!);
        }

        return returnValue;
    }
}

/// <summary>验证 CastleAsyncInterceptorAdapterTests 相关行为</summary>
public class CastleAsyncInterceptorAdapterTests
{
    /// <summary>验证 Type_ImplementsCastleAsyncInterceptor 场景</summary>
    [Fact]
    public void Type_ImplementsCastleAsyncInterceptor()
    {
        typeof(CastleAsyncInterceptorAdapter).Should().BeAssignableTo<IAsyncInterceptor>();
    }

    /// <summary>验证 Adapter_UsesSelectorServiceProviderAndPipeline_ForSyncTaskAndTaskOfTMethods 场景</summary>
    /// <returns>Adapter_UsesSelectorServiceProviderAndPipeline_ForSyncTaskAndTaskOfTMethods 的执行结果</returns>
    [Fact]
    public async Task Adapter_UsesSelectorServiceProviderAndPipeline_ForSyncTaskAndTaskOfTMethods()
    {
        var target = new AdapterTarget();
        var interceptor = new AdapterArgumentInterceptor();
        var services = new ServiceCollection()
            .AddSingleton<DependencyInterceptorSelector, AttributeInterceptorSelector>()
            .AddSingleton<IInterceptorPipeline, InterceptorPipeline>()
            .AddSingleton(interceptor)
            .BuildServiceProvider();
        var adapter = new CastleAsyncInterceptorAdapter(
            services.GetRequiredService<DependencyInterceptorSelector>(),
            services.GetRequiredService<IInterceptorPipeline>(),
            services);
        var proxy = CreateAdapterProxy(target, adapter);

        var syncResult = proxy.Sync("source");
        await proxy.TaskAsync("source");
        var taskResult = await proxy.TaskOfStringAsync("source");

        syncResult.Should().Be("intercepted:sync:adapter");
        taskResult.Should().Be("intercepted:task:adapter");
        target.ReceivedValues.Should().Equal("adapter", "adapter", "adapter");
        interceptor.MethodNames.Should().Equal("Sync", "TaskAsync", "TaskOfStringAsync");
    }

    /// <summary>验证 Adapter_DirectlyProceeds_WhenSelectorReturnsNoInterceptors 场景</summary>
    /// <returns>Adapter_DirectlyProceeds_WhenSelectorReturnsNoInterceptors 的执行结果</returns>
    [Fact]
    public async Task Adapter_DirectlyProceeds_WhenSelectorReturnsNoInterceptors()
    {
        var target = new AdapterTarget();
        var services = new ServiceCollection().BuildServiceProvider();
        var adapter = new CastleAsyncInterceptorAdapter(new EmptySelector(), new ThrowingPipeline(), services);
        var proxy = CreateAdapterProxy(target, adapter);

        var syncResult = proxy.Sync("source");
        await proxy.TaskAsync("source");
        var taskResult = await proxy.TaskOfStringAsync("source");

        syncResult.Should().Be("sync:source");
        taskResult.Should().Be("task:source");
        target.ReceivedValues.Should().Equal("source", "source", "source");
    }

    /// <summary>验证 Adapter_UsesSelectorServiceProviderAndPipeline_ForValueTaskMethods 场景</summary>
    /// <returns>Adapter_UsesSelectorServiceProviderAndPipeline_ForValueTaskMethods 的执行结果</returns>
    [Fact]
    public async Task Adapter_UsesSelectorServiceProviderAndPipeline_ForValueTaskMethods()
    {
        var target = new AdapterTarget();
        var interceptor = new AdapterArgumentInterceptor();
        var services = new ServiceCollection()
            .AddSingleton<DependencyInterceptorSelector, AttributeInterceptorSelector>()
            .AddSingleton<IInterceptorPipeline, InterceptorPipeline>()
            .AddSingleton(interceptor)
            .BuildServiceProvider();
        var adapter = new CastleAsyncInterceptorAdapter(
            services.GetRequiredService<DependencyInterceptorSelector>(),
            services.GetRequiredService<IInterceptorPipeline>(),
            services);
        var proxy = CreateAdapterProxy(target, adapter);

        await proxy.ValueTaskAsync("source");
        var valueTaskResult = await proxy.ValueTaskOfStringAsync("source");

        valueTaskResult.Should().Be("intercepted:value-task:adapter");
        target.ReceivedValues.Should().Equal("adapter", "adapter");
        interceptor.MethodNames.Should().Equal("ValueTaskAsync", "ValueTaskOfStringAsync");
    }

    /// <summary>验证 Adapter_DirectlyProceeds_ForValueTaskMethods_WhenSelectorReturnsNoInterceptors 场景</summary>
    /// <returns>Adapter_DirectlyProceeds_ForValueTaskMethods_WhenSelectorReturnsNoInterceptors 的执行结果</returns>
    [Fact]
    public async Task Adapter_DirectlyProceeds_ForValueTaskMethods_WhenSelectorReturnsNoInterceptors()
    {
        var target = new AdapterTarget();
        var services = new ServiceCollection().BuildServiceProvider();
        var adapter = new CastleAsyncInterceptorAdapter(new EmptySelector(), new ThrowingPipeline(), services);
        var proxy = CreateAdapterProxy(target, adapter);

        await proxy.ValueTaskAsync("source");
        var valueTaskResult = await proxy.ValueTaskOfStringAsync("source");

        valueTaskResult.Should().Be("value-task:source");
        target.ReceivedValues.Should().Equal("source", "source");
    }

    /// <summary>验证 CreateAdapterProxy 场景</summary>
    /// <param name="target">target 参数</param>
    /// <param name="adapter">adapter 参数</param>
    /// <returns>CreateAdapterProxy 的执行结果</returns>
    private static IAdapterTarget CreateAdapterProxy(AdapterTarget target, IAsyncInterceptor adapter)
    {
        var generator = new ProxyGenerator();
        var castleInterceptor = new AsyncDeterminationInterceptor(adapter);

        return generator.CreateInterfaceProxyWithTargetInterface<IAdapterTarget>(target, castleInterceptor);
    }

    /// <summary>定义 IAdapterTarget 契约</summary>
    [Intercept(typeof(AdapterArgumentInterceptor))]
    public interface IAdapterTarget
    {
        /// <summary>验证 Sync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Sync 的执行结果</returns>
        string Sync(string value);

        /// <summary>验证 TaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskAsync 的执行结果</returns>
        Task TaskAsync(string value);

        /// <summary>验证 TaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskOfStringAsync 的执行结果</returns>
        Task<string> TaskOfStringAsync(string value);

        /// <summary>验证 ValueTaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskAsync 的执行结果</returns>
        ValueTask ValueTaskAsync(string value);

        /// <summary>验证 ValueTaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskOfStringAsync 的执行结果</returns>
        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    /// <summary>验证 AdapterTarget 相关行为</summary>
    private sealed class AdapterTarget : IAdapterTarget
    {
        /// <summary>表示 ReceivedValues 属性</summary>
        public List<string> ReceivedValues { get; } = [];

        /// <summary>验证 Sync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>Sync 的执行结果</returns>
        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        /// <summary>验证 TaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskAsync 的执行结果</returns>
        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        /// <summary>验证 TaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>TaskOfStringAsync 的执行结果</returns>
        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        /// <summary>验证 ValueTaskAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskAsync 的执行结果</returns>
        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        /// <summary>验证 ValueTaskOfStringAsync 场景</summary>
        /// <param name="value">value 参数</param>
        /// <returns>ValueTaskOfStringAsync 的执行结果</returns>
        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    /// <summary>验证 AdapterArgumentInterceptor 相关行为</summary>
    private sealed class AdapterArgumentInterceptor : AbstractionInterceptor
    {
        /// <summary>表示 MethodNames 属性</summary>
        public List<string> MethodNames { get; } = [];

        /// <summary>验证 InterceptAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <returns>InterceptAsync 的执行结果</returns>
        public async ValueTask InterceptAsync(IInvocationContext context)
        {
            context.Arguments[0] = "adapter";
            await context.ProceedAsync();

            MethodNames.Add(context.Method.Name);
            if (context.ReturnValue is string value)
            {
                context.ReturnValue = $"intercepted:{value}";
            }
        }
    }

    /// <summary>验证 EmptySelector 相关行为</summary>
    private sealed class EmptySelector : DependencyInterceptorSelector
    {
        /// <summary>验证 SelectInterceptors 场景</summary>
        /// <param name="implementationType">implementationType 参数</param>
        /// <param name="serviceType">serviceType 参数</param>
        /// <param name="method">method 参数</param>
        /// <returns>SelectInterceptors 的执行结果</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, System.Reflection.MethodInfo method) =>
            [];
    }

    /// <summary>验证 ThrowingPipeline 相关行为</summary>
    private sealed class ThrowingPipeline : IInterceptorPipeline
    {
        /// <summary>验证 InvokeAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <param name="interceptors">interceptors 参数</param>
        /// <returns>InvokeAsync 的执行结果</returns>
        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<AbstractionInterceptor> interceptors) =>
            throw new InvalidOperationException("空拦截器链不应调用 pipeline");
    }
}
