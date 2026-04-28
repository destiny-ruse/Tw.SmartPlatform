namespace Tw.Core.Primitives;

/// <summary>
/// 为具名基元描述符提供可变列表
/// </summary>
/// <typeparam name="T">列表中存储的具名对象类型</typeparam>
public class NamedObjectList<T> : List<T>
    where T : NamedObject;
