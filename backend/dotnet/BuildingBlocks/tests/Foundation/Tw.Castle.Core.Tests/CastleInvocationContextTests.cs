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

/// <summary>
/// 覆盖Castle调用上下文的核心行为和边界条件
/// </summary>
public class CastleInvocationContextTests
{
    /// <summary>
    /// 验证继续处理异步Proceeds目标和捕获返回值针对SyncTask和值TaskMethods
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证继续处理异步写回参数Changes回到Castle调用
    /// </summary>
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

    /// <summary>
    /// 验证返回值Setter写回已修改值回到Castle调用
    /// </summary>
    [Fact]
    public void ReturnValueSetter_WritesModifiedValueBackToCastleInvocation()
    {
        var target = new InvocationTarget();
        var interceptor = new ReturnValueOverrideInterceptor();
        var proxy = CreateProxy(target, interceptor);

        var result = proxy.Sync("source");

        result.Should().Be("intercepted");
    }

    /// <summary>
    /// 验证继续处理抛出异常非法业务委托异常当目标方法返回Task
    /// </summary>
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

    /// <summary>
    /// 验证继续处理抛出异常非法业务委托异常当目标方法返回值Task
    /// </summary>
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

    /// <summary>
    /// 创建代理测试对象
    /// </summary>
    /// <param name="target">用于提供target</param>
    /// <param name="interceptor">用于提供nterceptor</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static IInvocationTarget CreateProxy(InvocationTarget target, CastleInterceptor interceptor)
    {
        var generator = new ProxyGenerator();

        return generator.CreateInterfaceProxyWithTargetInterface<IInvocationTarget>(target, interceptor);
    }

    /// <summary>
    /// 定义调用目标的能力边界
    /// </summary>
    public interface IInvocationTarget
    {
        /// <summary>
        /// 说明Sync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>方法计算得到的文本值</returns>
        string Sync(string value);

        /// <summary>
        /// 说明TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        Task TaskAsync(string value);

        /// <summary>
        /// 说明TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        Task<string> TaskOfStringAsync(string value);

        /// <summary>
        /// 说明值TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        ValueTask ValueTaskAsync(string value);

        /// <summary>
        /// 说明值TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    /// <summary>
    /// 覆盖调用Target的核心行为和边界条件
    /// </summary>
    private sealed class InvocationTarget : IInvocationTarget
    {
        /// <summary>
        /// Received值集合在当前对象中的业务含义
        /// </summary>
        public List<string> ReceivedValues { get; } = [];

        /// <summary>
        /// 说明Sync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>方法计算得到的文本值</returns>
        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        /// <summary>
        /// 说明TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 说明TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        /// <summary>
        /// 说明值TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 说明值TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    /// <summary>
    /// 覆盖继续处理异步Recording拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ProceedAsyncRecordingInterceptor : CastleInterceptor
    {
        /// <summary>
        /// 返回值集合在当前对象中的业务含义
        /// </summary>
        public List<object?> ReturnValues { get; } = [];

        /// <summary>
        /// nvocation参数值集合在当前对象中的业务含义
        /// </summary>
        public List<object?> InvocationArgumentValues { get; } = [];

        /// <summary>
        /// 说明ntercept在当前类型中的职责
        /// </summary>
        /// <param name="invocation">用于提供nvocation</param>
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

    /// <summary>
    /// 覆盖Return值Override拦截器的核心行为和边界条件
    /// </summary>
    private sealed class ReturnValueOverrideInterceptor : CastleInterceptor
    {
        /// <summary>
        /// 说明ntercept在当前类型中的职责
        /// </summary>
        /// <param name="invocation">用于提供nvocation</param>
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.ProceedAsync().AsTask().GetAwaiter().GetResult();
            context.ReturnValue = "intercepted";
        }
    }

    /// <summary>
    /// 覆盖Synchronous继续处理拦截器的核心行为和边界条件
    /// </summary>
    private sealed class SynchronousProceedInterceptor : CastleInterceptor
    {
        /// <summary>
        /// 说明ntercept在当前类型中的职责
        /// </summary>
        /// <param name="invocation">用于提供nvocation</param>
        public void Intercept(IInvocation invocation)
        {
            var context = new CastleInvocationContext(invocation);

            context.Proceed();
        }
    }

    /// <summary>
    /// 创建Compatible返回值测试对象
    /// </summary>
    /// <param name="returnType">用于提供return类型</param>
    /// <param name="returnValue">用于提供return值</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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

/// <summary>
/// 覆盖Castle异步拦截器Adapter的核心行为和边界条件
/// </summary>
public class CastleAsyncInterceptorAdapterTests
{
    /// <summary>
    /// 验证类型ImplementsCastle异步拦截器
    /// </summary>
    [Fact]
    public void Type_ImplementsCastleAsyncInterceptor()
    {
        typeof(CastleAsyncInterceptorAdapter).Should().BeAssignableTo<IAsyncInterceptor>();
    }

    /// <summary>
    /// 验证AdapterUsesSelector服务提供器和管道针对SyncTask和TaskOfTMethods
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证AdapterDirectlyProceeds当Selector返回No拦截器集合
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证AdapterUsesSelector服务提供器和管道针对值TaskMethods
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 验证AdapterDirectlyProceeds针对值TaskMethods当Selector返回No拦截器集合
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 创建Adapter代理测试对象
    /// </summary>
    /// <param name="target">用于提供target</param>
    /// <param name="adapter">用于提供adapter</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static IAdapterTarget CreateAdapterProxy(AdapterTarget target, IAsyncInterceptor adapter)
    {
        var generator = new ProxyGenerator();
        var castleInterceptor = new AsyncDeterminationInterceptor(adapter);

        return generator.CreateInterfaceProxyWithTargetInterface<IAdapterTarget>(target, castleInterceptor);
    }

    /// <summary>
    /// 定义Adapter目标的能力边界
    /// </summary>
    [Intercept(typeof(AdapterArgumentInterceptor))]
    public interface IAdapterTarget
    {
        /// <summary>
        /// 说明Sync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>方法计算得到的文本值</returns>
        string Sync(string value);

        /// <summary>
        /// 说明TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        Task TaskAsync(string value);

        /// <summary>
        /// 说明TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        Task<string> TaskOfStringAsync(string value);

        /// <summary>
        /// 说明值TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        ValueTask ValueTaskAsync(string value);

        /// <summary>
        /// 说明值TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        ValueTask<string> ValueTaskOfStringAsync(string value);
    }

    /// <summary>
    /// 覆盖AdapterTarget的核心行为和边界条件
    /// </summary>
    private sealed class AdapterTarget : IAdapterTarget
    {
        /// <summary>
        /// Received值集合在当前对象中的业务含义
        /// </summary>
        public List<string> ReceivedValues { get; } = [];

        /// <summary>
        /// 说明Sync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>方法计算得到的文本值</returns>
        public string Sync(string value)
        {
            ReceivedValues.Add(value);

            return $"sync:{value}";
        }

        /// <summary>
        /// 说明TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task TaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 说明TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        public Task<string> TaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return Task.FromResult($"task:{value}");
        }

        /// <summary>
        /// 说明值TaskAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask ValueTaskAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 说明值TaskOfStringAsync在当前类型中的职责
        /// </summary>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <returns>异步流程完成后产生的string</returns>
        public ValueTask<string> ValueTaskOfStringAsync(string value)
        {
            ReceivedValues.Add(value);

            return ValueTask.FromResult($"value-task:{value}");
        }
    }

    /// <summary>
    /// 覆盖AdapterArgument拦截器的核心行为和边界条件
    /// </summary>
    private sealed class AdapterArgumentInterceptor : AbstractionInterceptor
    {
        /// <summary>
        /// 方法名称集合在当前对象中的业务含义
        /// </summary>
        public List<string> MethodNames { get; } = [];

        /// <summary>
        /// 记录拦截调用并继续执行后续委托
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <returns>表示异步流程完成状态的任务</returns>
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

    /// <summary>
    /// 覆盖空Selector的核心行为和边界条件
    /// </summary>
    private sealed class EmptySelector : DependencyInterceptorSelector
    {
        /// <summary>
        /// 按服务类型和方法选择拦截器类型
        /// </summary>
        /// <param name="implementationType">服务注册中使用的实现类型</param>
        /// <param name="serviceType">服务注册中暴露的服务类型</param>
        /// <param name="method">用于构造测试场景的方法元数据</param>
        /// <returns>匹配当前查询条件的结果集合</returns>
        public IReadOnlyList<Type> SelectInterceptors(Type implementationType, Type serviceType, System.Reflection.MethodInfo method) =>
            [];
    }

    /// <summary>
    /// 覆盖Throwing管道的核心行为和边界条件
    /// </summary>
    private sealed class ThrowingPipeline : IInterceptorPipeline
    {
        /// <summary>
        /// 执行测试管道委托并记录调用
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <param name="interceptors">参与当前测试场景的拦截器集合</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<AbstractionInterceptor> interceptors) =>
            throw new InvalidOperationException("空拦截器链不应调用 pipeline");
    }
}
