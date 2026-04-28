using System.Collections.Concurrent;
using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// Provides cached reflection helpers for attributes and asynchronous method result types.
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
    /// Returns whether a member has the specified attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to locate.</typeparam>
    /// <param name="member">The member to inspect.</param>
    /// <param name="inherit">Whether inherited attributes should be included.</param>
    /// <returns><see langword="true"/> when the attribute is present; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
    public static bool HasAttribute<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetSingleAttributeOrNull<TAttribute>(inherit) is not null;
    }

    /// <summary>
    /// Gets the single matching attribute from a member, or <see langword="null"/> when none exists.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to locate.</typeparam>
    /// <param name="member">The member to inspect.</param>
    /// <param name="inherit">Whether inherited attributes should be included.</param>
    /// <returns>The first matching attribute, or <see langword="null"/> when the member does not have one.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
    public static TAttribute? GetSingleAttributeOrNull<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetAttributes<TAttribute>(inherit).FirstOrDefault();
    }

    /// <summary>
    /// Gets the single matching attribute from a member.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to locate.</typeparam>
    /// <param name="member">The member to inspect.</param>
    /// <param name="inherit">Whether inherited attributes should be included.</param>
    /// <returns>The first matching attribute.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the member does not have the requested attribute.</exception>
    public static TAttribute GetSingleAttribute<TAttribute>(this MemberInfo member, bool inherit = true)
        where TAttribute : Attribute
    {
        return member.GetSingleAttributeOrNull<TAttribute>(inherit)
            ?? throw new InvalidOperationException(
                $"Attribute {typeof(TAttribute).FullName} was not found on member {member.DeclaringType?.FullName}.{member.Name}.");
    }

    /// <summary>
    /// Gets all matching attributes from a member.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to locate.</typeparam>
    /// <param name="member">The member to inspect.</param>
    /// <param name="inherit">Whether inherited attributes should be included.</param>
    /// <returns>The matching attributes in reflection order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="member"/> is <see langword="null"/>.</exception>
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
    /// Returns whether a type is <see cref="Task"/>, <see cref="Task{TResult}"/>, <see cref="ValueTask"/>, or <see cref="ValueTask{TResult}"/>.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when the type is an asynchronous result type; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
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
    /// Gets the interfaces implemented by a type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The implemented interfaces in reflection order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static Type[] GetCachedInterfaces(this Type type)
    {
        var checkedType = Check.NotNull(type);

        return InterfacesCache.GetOrAdd(checkedType, static candidate => candidate.GetInterfaces()).ToArray();
    }

    /// <summary>
    /// Gets a public parameterless constructor from a type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The public parameterless constructor, or <see langword="null"/> when none exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static ConstructorInfo? GetCachedParameterlessCtor(this Type type)
    {
        var checkedType = Check.NotNull(type);

        return ParameterlessConstructorCache.GetOrAdd(checkedType, static candidate => candidate.GetConstructor(Type.EmptyTypes));
    }

    /// <summary>
    /// Returns whether a type has a public parameterless constructor.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> when a public parameterless constructor exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <see langword="null"/>.</exception>
    public static bool HasParameterlessCtor(this Type type)
    {
        return type.GetCachedParameterlessCtor() is not null;
    }

    /// <summary>
    /// Returns whether a method returns a task or value task type.
    /// </summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns><see langword="true"/> when the method return type is asynchronous; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is <see langword="null"/>.</exception>
    public static bool IsAsyncMethod(this MethodInfo method)
    {
        var checkedMethod = Check.NotNull(method);

        return MethodIsAsyncCache.GetOrAdd(checkedMethod, static methodInfo => methodInfo.ReturnType.IsAsyncReturnType());
    }

    /// <summary>
    /// Gets the logical result type for a method, unwrapping <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/>.
    /// </summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns>
    /// The generic result type for <see cref="Task{TResult}"/> and <see cref="ValueTask{TResult}"/>,
    /// <see cref="void"/> for non-generic <see cref="Task"/> and <see cref="ValueTask"/>,
    /// or the declared return type for non-task methods.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> is <see langword="null"/>.</exception>
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
    /// Gets reflection cache entry counts for diagnostics in debug builds.
    /// </summary>
    /// <returns>The current cache entry counts.</returns>
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
    /// Represents reflection cache entry counts for diagnostics in debug builds.
    /// </summary>
    /// <param name="AttributeCacheCount">The attribute cache entry count.</param>
    /// <param name="AsyncReturnTypeCacheCount">The asynchronous return type cache entry count.</param>
    /// <param name="InterfacesCacheCount">The implemented interfaces cache entry count.</param>
    /// <param name="ParameterlessConstructorCacheCount">The parameterless constructor cache entry count.</param>
    /// <param name="MethodIsAsyncCacheCount">The asynchronous method cache entry count.</param>
    /// <param name="AsyncResultTypeCacheCount">The asynchronous result type cache entry count.</param>
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
