namespace Tw.Core.Reflection;

/// <summary>
/// 为 <see cref="ITypeFinder"/> 提供不依赖额外组件的便利方法
/// </summary>
public static class TypeFinderExtensions
{
    /// <summary>
    /// 从配置的类型查找器中查找所有具体类型
    /// </summary>
    /// <param name="typeFinder">要查询的类型查找器</param>
    /// <returns>按发现顺序排列的具体类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="typeFinder"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<Type> FindConcreteTypes(this ITypeFinder typeFinder)
    {
        return Check.NotNull(typeFinder)
            .FindTypes()
            .Where(IsConcrete);
    }

    /// <summary>
    /// 查找可赋值给给定基类型参数的具体类型
    /// </summary>
    /// <typeparam name="TBaseType">发现的类型必须实现的基类型或接口</typeparam>
    /// <param name="typeFinder">要查询的类型查找器</param>
    /// <returns>按发现顺序排列的匹配具体类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="typeFinder"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<Type> FindConcreteTypesAssignableTo<TBaseType>(this ITypeFinder typeFinder)
    {
        return FindConcreteTypesAssignableTo(typeFinder, typeof(TBaseType));
    }

    /// <summary>
    /// 查找可赋值给给定基类型的具体类型
    /// </summary>
    /// <param name="typeFinder">要查询的类型查找器</param>
    /// <param name="baseType">发现的类型必须实现的基类型或接口</param>
    /// <returns>按发现顺序排列的匹配具体类型</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="typeFinder"/> 或 <paramref name="baseType"/> 为 <see langword="null"/> 时抛出</exception>
    public static IEnumerable<Type> FindConcreteTypesAssignableTo(this ITypeFinder typeFinder, Type baseType)
    {
        Check.NotNull(baseType);

        return Check.NotNull(typeFinder).FindTypes(baseType);
    }

    private static bool IsConcrete(Type type)
    {
        return !type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters;
    }
}
