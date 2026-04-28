namespace Tw.Core.Extensions;

/// <summary>提供 <see cref="Type"/> 值扩展方法</summary>
public static class TypeExtensions
{
    /// <summary>返回完整类型名以及程序集名称</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static string GetFullNameWithAssemblyName(this Type type)
    {
        var checkedType = Check.NotNull(type);
        return $"{checkedType.FullName}, {checkedType.Assembly.GetName().Name}";
    }

    /// <summary>返回类型是否可赋值给目标类型</summary>
    /// <typeparam name="TTarget">目标类型</typeparam>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool IsAssignableTo<TTarget>(this Type type)
    {
        return IsAssignableTo(type, typeof(TTarget));
    }

    /// <summary>返回类型是否可赋值给目标类型</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 或 <paramref name="targetType"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool IsAssignableTo(this Type type, Type targetType)
    {
        return Check.NotNull(targetType).IsAssignableFrom(Check.NotNull(type));
    }

    /// <summary>获取类型的基类</summary>
    /// <param name="type">源类型</param>
    /// <param name="includeObject">是否包含 <see cref="object"/></param>
    /// <returns>从最近到最远排列的基类</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static Type[] GetBaseClasses(this Type type, bool includeObject = true)
    {
        return type.GetBaseClasses(stoppingType: null!, includeObject);
    }

    /// <summary>获取类型的基类，并在指定类型之前停止</summary>
    /// <param name="type">源类型</param>
    /// <param name="stoppingType">遍历应在返回此基类型之前停止</param>
    /// <param name="includeObject">是否包含 <see cref="object"/></param>
    /// <returns>从最近到最远排列的基类</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true)
    {
        Check.NotNull(type);

        var baseClasses = new List<Type>();
        var current = type.BaseType;
        while (current is not null)
        {
            if (current == stoppingType || (!includeObject && current == typeof(object)))
            {
                break;
            }

            baseClasses.Add(current);
            current = current.BaseType;
        }

        return baseClasses.ToArray();
    }
}
