using Castle.DynamicProxy;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.DependencyInjection.DynamicProxy;
using Tw.DynamicProxy.Abstractions;
using Xunit;
using AbstractionInterceptor = Tw.DynamicProxy.Abstractions.IInterceptor;
using CastleInterceptor = Castle.DynamicProxy.IInterceptor;
using DependencyInterceptorSelector = Tw.DependencyInjection.DynamicProxy.IInterceptorSelector;

namespace Tw.DependencyInjection.Tests.DynamicProxy;

public class CastleInvocationContextTests
{
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

    [Fact]
    public void ReturnValueSetter_WritesModifiedValueBackToCastleInvocation()
    {
        var target = new InvocationTarget();
        var interceptor = new ReturnValueOverrideInterceptor();
        var proxy = CreateProxy(target, interceptor);

        var result = proxy.Sync("source");

        result.Should().Be("intercepted");
    }

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

    private static IInvocationTarget CreateProxy(InvocationTarget target, CastleInterceptor interceptor)
    {
        var generator = new ProxyGenerator();

        return generator.CreateInterfaceProxyWithTargetInterface<IInvocationTarget>(target, interceptor);
    }

    public interface IInvocationTarget
    {
        string Sync(string value);

        Task TaskAsync(string value);

        Task<string> TaskOfStringAsync(string value);

        ValueTask ValueTaskAsync(string value);

        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    private sealed class InvocationTarget : IInvocationTarget
    {
        public List<string> ReceivedValues { get; } = [];

        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    private sealed class ProceedAsyncRecordingInterceptor : CastleInterceptor
    {
        public List<object?> ReturnValues { get; } = [];

        public List<object?> InvocationArgumentValues { get; } = [];

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

    private sealed class ReturnValueOverrideInterceptor : CastleInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.ProceedAsync().AsTask().GetAwaiter().GetResult();
            context.ReturnValue = "intercepted";
        }
    }

    private sealed class SynchronousProceedInterceptor : CastleInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.Proceed();
        }
    }

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

public class CastleAsyncInterceptorAdapterTests
{
    [Fact]
    public void Type_ImplementsCastleAsyncInterceptor()
    {
        typeof(CastleAsyncInterceptorAdapter).Should().BeAssignableTo<IAsyncInterceptor>();
    }

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

    private static IAdapterTarget CreateAdapterProxy(AdapterTarget target, IAsyncInterceptor adapter)
    {
        var generator = new ProxyGenerator();
        var castleInterceptor = new AsyncDeterminationInterceptor(adapter);

        return generator.CreateInterfaceProxyWithTargetInterface<IAdapterTarget>(target, castleInterceptor);
    }

    [Intercept(typeof(AdapterArgumentInterceptor))]
    public interface IAdapterTarget
    {
        string Sync(string value);

        Task TaskAsync(string value);

        Task<string> TaskOfStringAsync(string value);

        ValueTask ValueTaskAsync(string value);

        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    private sealed class AdapterTarget : IAdapterTarget
    {
        public List<string> ReceivedValues { get; } = [];

        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    private sealed class AdapterArgumentInterceptor : AbstractionInterceptor
    {
        public List<string> MethodNames { get; } = [];

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

    private sealed class EmptySelector : DependencyInterceptorSelector
    {
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, System.Reflection.MethodInfo method) =>
            [];
    }

    private sealed class ThrowingPipeline : IInterceptorPipeline
    {
        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<AbstractionInterceptor> interceptors) =>
            throw new InvalidOperationException("空拦截器链不应调用 pipeline");
    }
}
