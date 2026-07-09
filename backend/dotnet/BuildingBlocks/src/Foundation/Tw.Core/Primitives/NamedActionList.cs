namespace Tw.Core.Primitives;

/// <summary>
/// 为具名操作提供可变列表
/// </summary>
/// <typeparam name="T">操作参数类型</typeparam>
public class NamedActionList<T> : NamedObjectList<NamedAction<T>>;
