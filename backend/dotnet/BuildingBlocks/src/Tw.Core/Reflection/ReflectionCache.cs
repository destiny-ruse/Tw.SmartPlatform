using System.Collections.Concurrent;
using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// 为特性和异步方法结果类型提供带缓存的反射辅助方法
/// </summary>
public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<AttributeCacheKey, IReadOnlyList<Attribute>> AttributeCache = new();
    private static readonly ConcurrentDictionary<Type, bool> AsyncReturnTypeCache = new();
    private static readonly ConcurrentDictionary<Type, Type[]> InterfacesCache = new();
    private static readonly ConcurrentDictionary<Type, ConstructorInfo?> ParameterlessConstructorCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, bool> MethodIsAsyncCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, Type> AsyncResultTypeCache = new();

    /// <summary>
    /// 返回成员是否具有指定特性
    /// </summary>
    /// <typeparam name="TAttribute">要定位的特性类型</typeparam>
    /// <param name="member">要检查的成员</param>
    /// <param name="inherit">是否包含继承的特性</param>
    /// <returns>特性存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="member"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool HasAttribute<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetSingleAttributeOrNull<TAttribute>(inherit) is not null;
    }

    /// <summary>
    /// 从成员获取单个匹配特性；不存在时返回 <see langword="null"/>
    /// </summary>
    /// <typeparam name="TAttribute">要定位的特性类型</typeparam>
    /// <param name="member">要检查的成员</param>
    /// <param name="inherit">是否包含继承的特性</param>
    /// <returns>第一个匹配特性；成员没有该特性时返回 <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="member"/> 为 <see langword="null"/> 时抛出</exception>
    public static TAttribute? GetSingleAttributeOrNull<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetAttributes<TAttribute>(inherit).FirstOrDefault();
    }

    /// <summary>
    /// 从成员获取单个匹配特性
    /// </summary>
    /// <typeparam name="TAttribute">要定位的特性类型</typeparam>
    /// <param name="member">要检查的成员</param>
    /// <param name="inherit">是否包含继承的特性</param>
    /// <returns>第一个匹配特性</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="member"/> 为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="InvalidOperationException">当成员不具有请求的特性时抛出</exception>
    public static TAttribute GetSingleAttribute<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetSingleAttributeOrNull<TAttribute>(inherit)
            ?? throw new InvalidOperationException(
                $"未在成员 {member.DeclaringType?.FullName}.{member.Name} 上找到特性 {typeof(TAttribute).FullName}。");
    }

    /// <summary>
    /// 从成员获取所有匹配特性
    /// </summary>
    /// <typeparam name="TAttribute">要定位的特性类型</typeparam>
    /// <param name="member">要检查的成员</param>
    /// <param name="inherit">是否包含继承的特性</param>
    /// <returns>按反射顺序排列的匹配特性</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="member"/> 为 <see langword="null"/> 时抛出</exception>
    public static IReadOnlyList<TAttribute> GetAttributes<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        var checkedMember = Check.NotNull(member);
        var key = new AttributeCacheKey(checkedMember, typeof(TAttribute), inherit);

        return AttributeCache
            .GetOrAdd(key, static cacheKey => Attribute
                .GetCustomAttributes(cacheKey.Member, cacheKey.AttributeType, cacheKey.Inherit)
                .ToArray())
            .Cast<TAttribute>()
            .ToArray();
    }

    /// <summary>
    /// 返回类型是否为 <see cref="Task"/>、<see cref="Task{TResult}"/>、<see cref="ValueTask"/> 或 <see cref="ValueTask{TResult}"/>
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>类型是异步结果类型时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool IsAsyncReturnType(this Type type)
    {
        var checkedType = Check.NotNull(type);

        return AsyncReturnTypeCache.GetOrAdd(checkedType, static candidate =>
        {
            if (candidate == typeof(Task) || candidate == typeof(ValueTask))
            {
                return true;
            }

            if (!candidate.IsGenericType)
            {
                return false;
            }

            var genericTypeDefinition = candidate.GetGenericTypeDefinition();

            return genericTypeDefinition == typeof(Task<>)
                || genericTypeDefinition == typeof(ValueTask<>);
        });
    }

    /// <summary>
    /// 获取类型实现的接口
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>按反射顺序排列的已实现接口</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static Type[] GetCachedInterfaces(this Type type)
    {
        var checkedType = Check.NotNull(type);

        return InterfacesCache.GetOrAdd(checkedType, static candidate => candidate.GetInterfaces()).ToArray();
    }

    /// <summary>
    /// 从类型获取公共无参构造函数
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>公共无参构造函数；不存在时返回 <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static ConstructorInfo? GetCachedParameterlessCtor(this Type type)
    {
        var checkedType = Check.NotNull(type);

        return ParameterlessConstructorCache.GetOrAdd(checkedType, static candidate => candidate.GetConstructor(Type.EmptyTypes));
    }

    /// <summary>
    /// 返回类型是否具有公共无参构造函数
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>公共无参构造函数存在时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="type"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool HasParameterlessCtor(this Type type)
    {
        return type.GetCachedParameterlessCtor() is not null;
    }

    /// <summary>
    /// 返回方法是否返回任务或值任务类型
    /// </summary>
    /// <param name="method">要检查的方法</param>
    /// <returns>方法返回类型为异步类型时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="method"/> 为 <see langword="null"/> 时抛出</exception>
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        var checkedMethod = Check.NotNull(method);

        return MethodIsAsyncCache.GetOrAdd(checkedMethod, static methodInfo => methodInfo.ReturnType.IsAsyncReturnType());
    }

    /// <summary>
    /// 获取方法的逻辑结果类型，并展开 <see cref="Task{TResult}"/> 与 <see cref="ValueTask{TResult}"/>
    /// </summary>
    /// <param name="method">要检查的方法</param>
    /// <returns>
    /// 对 <see cref="Task{TResult}"/> 与 <see cref="ValueTask{TResult}"/> 返回泛型结果类型；
    /// 对非泛型 <see cref="Task"/> 与 <see cref="ValueTask"/> 返回 <see cref="void"/>；
    /// 对非任务方法返回声明的返回类型
    /// </returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="method"/> 为 <see langword="null"/> 时抛出</exception>
    public static Type GetAsyncResultType(this MethodInfo method)
    {
        var checkedMethod = Check.NotNull(method);

        return AsyncResultTypeCache.GetOrAdd(checkedMethod, static methodInfo =>
        {
            var returnType = methodInfo.ReturnType;
            if (returnType == typeof(Task) || returnType == typeof(ValueTask))
            {
                return typeof(void);
            }

            if (returnType.IsGenericType)
            {
                var genericTypeDefinition = returnType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Task<>) || genericTypeDefinition == typeof(ValueTask<>))
                {
                    return returnType.GetGenericArguments()[0];
                }
            }

            return returnType;
        });
    }

#if DEBUG
    /// <summary>
    /// 获取调试构建中用于诊断的反射缓存条目数量
    /// </summary>
    /// <returns>当前缓存条目数量</returns>
    public static CacheStatistics GetStatistics()
    {
        return new CacheStatistics(
            AttributeCacheCount: AttributeCache.Count,
            AsyncReturnTypeCacheCount: AsyncReturnTypeCache.Count,
            InterfacesCacheCount: InterfacesCache.Count,
            ParameterlessConstructorCacheCount: ParameterlessConstructorCache.Count,
            MethodIsAsyncCacheCount: MethodIsAsyncCache.Count,
            AsyncResultTypeCacheCount: AsyncResultTypeCache.Count);
    }

    /// <summary>
    /// 表示调试构建中用于诊断的反射缓存条目数量
    /// </summary>
    /// <param name="AttributeCacheCount">特性缓存条目数量</param>
    /// <param name="AsyncReturnTypeCacheCount">异步返回类型缓存条目数量</param>
    /// <param name="InterfacesCacheCount">已实现接口缓存条目数量</param>
    /// <param name="ParameterlessConstructorCacheCount">无参构造函数缓存条目数量</param>
    /// <param name="MethodIsAsyncCacheCount">异步方法缓存条目数量</param>
    /// <param name="AsyncResultTypeCacheCount">异步结果类型缓存条目数量</param>
    public sealed record CacheStatistics(
        int AttributeCacheCount,
        int AsyncReturnTypeCacheCount,
        int InterfacesCacheCount,
        int ParameterlessConstructorCacheCount,
        int MethodIsAsyncCacheCount,
        int AsyncResultTypeCacheCount);
#endif

    private readonly record struct AttributeCacheKey(MemberInfo Member, Type AttributeType, bool Inherit);
}
