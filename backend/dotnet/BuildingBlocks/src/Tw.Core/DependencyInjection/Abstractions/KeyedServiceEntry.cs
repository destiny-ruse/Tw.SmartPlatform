namespace Tw.DependencyInjection.Abstractions;

/// <summary>
/// 携带 key 元数据的 keyed 服务条目，用于枚举某契约的全部 keyed 实现
/// </summary>
/// <remarks>
/// 相等比较逐字段进行：<c>Key</c> 与 <c>Service</c> 均使用引用相等（<see langword="object"/> 默认行为）。
/// 该类型仅用于枚举消费，不应依赖其相等语义做集合去重或分组。
/// </remarks>
/// <typeparam name="TService">服务契约类型</typeparam>
/// <param name="Key">注册时声明的稳定 key；不得为 null，引擎在注册规划阶段校验</param>
/// <param name="Service">该 key 对应的服务实例</param>
public readonly record struct KeyedServiceEntry<TService>(object Key, TService Service)
    where TService : notnull;
