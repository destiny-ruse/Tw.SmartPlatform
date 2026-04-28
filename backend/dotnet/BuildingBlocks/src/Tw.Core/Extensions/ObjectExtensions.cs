namespace Tw.Core.Extensions;

/// <summary>提供通用对象值扩展方法</summary>
public static class ObjectExtensions
{
    /// <summary>将对象转换为引用类型</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="obj"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidCastException">当 <paramref name="obj"/> 不能赋值给 <typeparamref name="T"/> 时抛出</exception>
    public static T As<T>(this object obj)
        where T : class
    {
        var value = Check.NotNull(obj);
        if (value is T typedValue)
        {
            return typedValue;
        }

        throw new InvalidCastException(
            $"类型 {value.GetType().FullName} 的对象不能转换为 {typeof(T).FullName}。");
    }

    /// <summary>将对象转换为值类型</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="obj"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidCastException">当不支持转换时抛出</exception>
    /// <exception cref="FormatException">当转换输入格式无效时抛出</exception>
    public static T To<T>(this object obj)
        where T : struct
    {
        Check.NotNull(obj);
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(obj, targetType);
    }

    /// <summary>返回元素是否包含在给定值中</summary>
    public static bool IsIn<T>(this T item, params T[] list)
    {
        return item.IsIn((IEnumerable<T>)Check.NotNull(list));
    }

    /// <summary>返回元素是否包含在给定枚举中</summary>
    public static bool IsIn<T>(this T item, IEnumerable<T> items)
    {
        return Check.NotNull(items).Contains(item);
    }

    /// <summary>当条件为真时转换值</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="func"/> 为 <see langword="null"/> 时抛出</exception>
    public static T If<T>(this T obj, bool condition, Func<T, T> func)
    {
        Check.NotNull(func);
        return condition ? func(obj) : obj;
    }

    /// <summary>当条件为真时为值调用操作，并返回原始值</summary>
    /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 <see langword="null"/> 时抛出</exception>
    public static T If<T>(this T obj, bool condition, Action<T> action)
    {
        Check.NotNull(action);

        if (condition)
        {
            action(obj);
        }

        return obj;
    }
}
