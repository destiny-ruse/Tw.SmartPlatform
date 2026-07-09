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

    /// <summary>表示 PipelineExecution 类型</summary>
    private sealed class PipelineExecution
    {
        /// <summary>表示 _innerContext 字段</summary>
        private readonly IInvocationContext _innerContext;
        /// <summary>表示 _interceptors 字段</summary>
        private readonly IReadOnlyList<IInterceptor> _interceptors;
        /// <summary>表示 _hasProceededTarget 字段</summary>
        private bool _hasProceededTarget;

        /// <summary>初始化 PipelineExecution 实例</summary>
        /// <param name="innerContext">innerContext 参数</param>
        /// <param name="interceptors">interceptors 参数</param>
        public PipelineExecution(IInvocationContext innerContext, IReadOnlyList<IInterceptor> interceptors)
        {
            _innerContext = innerContext;
            _interceptors = interceptors;
        }

        /// <summary>执行 ProceedAsync 操作</summary>
        /// <param name="nextInterceptorIndex">nextInterceptorIndex 参数</param>
        /// <returns>ProceedAsync 的执行结果</returns>
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

        /// <summary>执行 Proceed 操作</summary>
        /// <param name="nextInterceptorIndex">nextInterceptorIndex 参数</param>
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

        /// <summary>执行 EnsureTargetHasNotProceeded 操作</summary>
        private void EnsureTargetHasNotProceeded()
        {
            if (_hasProceededTarget)
            {
                throw new InvalidOperationException("目标方法已推进，不能在同一调用链中重复调用 Proceed");
            }

            _hasProceededTarget = true;
        }
    }

    /// <summary>表示 PipelineInvocationContext 类型</summary>
    private sealed class PipelineInvocationContext : IInvocationContext
    {
        /// <summary>表示 _innerContext 字段</summary>
        private readonly IInvocationContext _innerContext;
        /// <summary>表示 _pipelineExecution 字段</summary>
        private readonly PipelineExecution _pipelineExecution;
        /// <summary>表示 _nextInterceptorIndex 字段</summary>
        private readonly int _nextInterceptorIndex;
        /// <summary>表示 _hasProceededFrame 字段</summary>
        private bool _hasProceededFrame;

        /// <summary>初始化 PipelineInvocationContext 实例</summary>
        /// <param name="innerContext">innerContext 参数</param>
        /// <param name="pipelineExecution">pipelineExecution 参数</param>
        /// <param name="nextInterceptorIndex">nextInterceptorIndex 参数</param>
        public PipelineInvocationContext(
            IInvocationContext innerContext,
            PipelineExecution pipelineExecution,
            int nextInterceptorIndex)
        {
            _innerContext = innerContext;
            _pipelineExecution = pipelineExecution;
            _nextInterceptorIndex = nextInterceptorIndex;
        }

        /// <summary>表示 Method 属性</summary>
        public MethodInfo Method => _innerContext.Method;

        /// <summary>表示 Target 属性</summary>
        public object? Target => _innerContext.Target;

        /// <summary>表示 Arguments 属性</summary>
        public object?[] Arguments => _innerContext.Arguments;

        /// <summary>表示 ArgumentsByName 属性</summary>
        public IReadOnlyDictionary<string, object?> ArgumentsByName => _innerContext.ArgumentsByName;

        /// <summary>表示 ReturnValue 属性</summary>
        public object? ReturnValue
        {
            get => _innerContext.ReturnValue;
            set => _innerContext.ReturnValue = value;
        }

        /// <summary>执行 ProceedAsync 操作</summary>
        /// <returns>ProceedAsync 的执行结果</returns>
        public ValueTask ProceedAsync()
        {
            EnsureFrameHasNotProceeded();

            return _pipelineExecution.ProceedAsync(_nextInterceptorIndex);
        }

        /// <summary>执行 Proceed 操作</summary>
        public void Proceed()
        {
            EnsureFrameHasNotProceeded();

            _pipelineExecution.Proceed(_nextInterceptorIndex);
        }

        /// <summary>执行 EnsureFrameHasNotProceeded 操作</summary>
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
