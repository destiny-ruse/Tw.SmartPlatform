namespace Tw.DynamicProxy.Abstractions;

/// <summary>
/// 声明拦截器在调用链中的顺序
/// </summary>
/// <remarks>顺序相同按类型名称稳定排序。</remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InterceptorOrderAttribute : Attribute
{
    /// <summary>声明拦截器顺序</summary>
    /// <param name="order">顺序数值，越小越先执行</param>
    public InterceptorOrderAttribute(int order)
    {
        Order = order;
    }

    /// <summary>拦截器顺序</summary>
    public int Order { get; }
}
