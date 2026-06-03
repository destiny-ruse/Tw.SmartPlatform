using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// 从配置的程序集集合中查找运行时类型
/// </summary>
public interface ITypeFinder
{
    /// <summary>
    /// 用于搜索类型的程序集
    /// </summary>
    IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>
    /// 从已配置程序集中查找可加载类型
    /// </summary>
    /// <returns>按程序集遍历顺序发现的类型</returns>
    IReadOnlyList<Type> FindTypes();

    /// <summary>
    /// 查找可赋值给给定基类型的具体类型
    /// </summary>
    /// <param name="baseType">发现的类型必须实现的基类型或接口</param>
    /// <returns>按发现顺序排列的匹配具体类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="baseType"/> 为 <see langword="null"/> 时抛出</exception>
    IReadOnlyList<Type> FindTypes(Type baseType);

    /// <summary>
    /// 查找可赋值给给定基类型参数的具体类型
    /// </summary>
    /// <typeparam name="TBaseType">发现的类型必须实现的基类型或接口</typeparam>
    /// <returns>按发现顺序排列的匹配具体类型</returns>
    IReadOnlyList<Type> FindTypes<TBaseType>();
}
