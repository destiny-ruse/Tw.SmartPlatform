using System.Reflection;
using Tw.Castle.Core.Abstractions;

namespace Tw.Castle.Core;

/// <summary>
/// 基于调用上下文包装的拦截器管道
/// </summary>
/// <remarks>
/// 管道允许拦截器短路目标调用；同一拦截器调用帧重复调用 Proceed 会抛出异常，避免绕过短路节点或重复执行目标方法。
/// </remarks>
public sealed class InterceptorPipeline : IInterceptorPipeline
{
    /// <summary>
    /// 按顺序执行拦截器链，并保证真实目标方法最多推进一次
    /// </summary>
    /// <param name="context">原始方法级调用上下文</param>
    /// <param name="interceptors">按执行顺序排列的拦截器实例；为空时直接推进目标方法一次</param>
    /// <returns>表示拦截链执行完成的 <see cref="ValueTask"/></returns>
    /// <exception cref="ArgumentNullException">context 或 interceptors 为 null 时抛出</exception>
    /// <exception cref="InvalidOperationException">同一拦截器调用帧重复调用 Proceed，或真实目标方法已推进后再次推进目标时抛出</exception>
    public ValueTask InvokeAsync(IInvocationContext context, IReadOnlyList<IInterceptor> interceptors)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interceptors);

        var pipelineExecution = new PipelineExecution(context, interceptors);

        return pipelineExecution.ProceedAsync(nextInterceptorIndex: 0);
    }

    /// <summary>
    /// 封装管道Execution相关的数据和行为
    /// </summary>
    private sealed class PipelineExecution
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的inner上下文
        /// </summary>
        private readonly IInvocationContext _innerContext;
        /// <summary>
        /// 保存当前类型处理流程依赖的interceptors
        /// </summary>
        private readonly IReadOnlyList<IInterceptor> _interceptors;
        /// <summary>
        /// 保存当前类型处理流程依赖的hasProceededTarget
        /// </summary>
        private bool _hasProceededTarget;

        /// <summary>
        /// 初始化 PipelineExecution 实例
        /// </summary>
        /// <param name="innerContext">用于提供nner上下文</param>
        /// <param name="interceptors">参与当前测试场景的拦截器集合</param>
        public PipelineExecution(IInvocationContext innerContext, IReadOnlyList<IInterceptor> interceptors)
        {
            _innerContext = innerContext;
            _interceptors = interceptors;
        }

        /// <summary>
        /// 说明ProceedAsync在当前类型中的职责
        /// </summary>
        /// <param name="nextInterceptorIndex">用于提供next拦截器Index</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask ProceedAsync(int nextInterceptorIndex)
        {
            if (nextInterceptorIndex >= _interceptors.Count)
            {
                EnsureTargetHasNotProceeded();

                return _innerContext.ProceedAsync();
            }

            var interceptor = _interceptors[nextInterceptorIndex];
            var invocationContext = new PipelineInvocationContext(
                _innerContext,
                this,
                nextInterceptorIndex + 1);

            return interceptor.InterceptAsync(invocationContext);
        }

        /// <summary>
        /// 说明Proceed在当前类型中的职责
        /// </summary>
        /// <param name="nextInterceptorIndex">用于提供next拦截器Index</param>
        public void Proceed(int nextInterceptorIndex)
        {
            if (nextInterceptorIndex >= _interceptors.Count)
            {
                EnsureTargetHasNotProceeded();
                _innerContext.Proceed();

                return;
            }

            var interceptor = _interceptors[nextInterceptorIndex];
            var invocationContext = new PipelineInvocationContext(
                _innerContext,
                this,
                nextInterceptorIndex + 1);

            interceptor.InterceptAsync(invocationContext).AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 说明EnsureTarget存在不Proceeded在当前类型中的职责
        /// </summary>
        private void EnsureTargetHasNotProceeded()
        {
            if (_hasProceededTarget)
            {
                throw new InvalidOperationException("目标方法已推进，不能在同一调用链中重复调用 Proceed");
            }

            _hasProceededTarget = true;
        }
    }

    /// <summary>
    /// 封装管道调用上下文相关的数据和行为
    /// </summary>
    private sealed class PipelineInvocationContext : IInvocationContext
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的inner上下文
        /// </summary>
        private readonly IInvocationContext _innerContext;
        /// <summary>
        /// 保存当前类型处理流程依赖的管道Execution
        /// </summary>
        private readonly PipelineExecution _pipelineExecution;
        /// <summary>
        /// 保存当前类型处理流程依赖的next拦截器Index
        /// </summary>
        private readonly int _nextInterceptorIndex;
        /// <summary>
        /// 保存当前类型处理流程依赖的hasProceededFrame
        /// </summary>
        private bool _hasProceededFrame;

        /// <summary>
        /// 初始化 PipelineInvocationContext 实例
        /// </summary>
        /// <param name="innerContext">用于提供nner上下文</param>
        /// <param name="pipelineExecution">用于提供pipelineExecution</param>
        /// <param name="nextInterceptorIndex">用于提供next拦截器Index</param>
        public PipelineInvocationContext(
            IInvocationContext innerContext,
            PipelineExecution pipelineExecution,
            int nextInterceptorIndex)
        {
            _innerContext = innerContext;
            _pipelineExecution = pipelineExecution;
            _nextInterceptorIndex = nextInterceptorIndex;
        }

        /// <summary>
        /// 方法在当前对象中的业务含义
        /// </summary>
        public MethodInfo Method => _innerContext.Method;

        /// <summary>
        /// 目标在当前对象中的业务含义
        /// </summary>
        public object? Target => _innerContext.Target;

        /// <summary>
        /// 参数在当前对象中的业务含义
        /// </summary>
        public object?[] Arguments => _innerContext.Arguments;

        /// <summary>
        /// 当前调用按名称索引后的参数集合
        /// </summary>
        public IReadOnlyDictionary<string, object?> ArgumentsByName => _innerContext.ArgumentsByName;

        /// <summary>
        /// 当前对象用于完成处理流程的内部状态
        /// </summary>
        public object? ReturnValue
        {
            get => _innerContext.ReturnValue;
            set => _innerContext.ReturnValue = value;
        }

        /// <summary>
        /// 说明ProceedAsync在当前类型中的职责
        /// </summary>
        /// <returns>表示异步流程完成状态的任务</returns>
        public ValueTask ProceedAsync()
        {
            EnsureFrameHasNotProceeded();

            return _pipelineExecution.ProceedAsync(_nextInterceptorIndex);
        }

        /// <summary>
        /// 说明Proceed在当前类型中的职责
        /// </summary>
        public void Proceed()
        {
            EnsureFrameHasNotProceeded();

            _pipelineExecution.Proceed(_nextInterceptorIndex);
        }

        /// <summary>
        /// 说明EnsureFrame存在不Proceeded在当前类型中的职责
        /// </summary>
        private void EnsureFrameHasNotProceeded()
        {
            if (_hasProceededFrame)
            {
                throw new InvalidOperationException("当前拦截器调用帧已调用 Proceed，不能重复调用 Proceed");
            }

            _hasProceededFrame = true;
        }
    }
}
