namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 携带 key 元数据的 keyed 服务条目，用于枚举某契约的全部 keyed 实现
/// </summary>
/// <typeparam name="TService">服务契约类型</typeparam>
/// <param name="Key">注册时声明的稳定 key</param>
/// <param name="Service">该 key 对应的服务实例</param>
public readonly record struct KeyedServiceEntry<TService>(object Key, TService Service)
    where TService : notnull;
