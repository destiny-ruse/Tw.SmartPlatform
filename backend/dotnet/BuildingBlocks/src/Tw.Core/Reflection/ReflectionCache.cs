using System.Collections.Concurrent;
using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// Provides cached reflection helpers for attributes and asynchronous method result types.
/// </summary>
public static class ReflectionCache
{
    private static readonly ConcurrentDictionary<AttributeCacheKey, IReadOnlyList<Attribute>> AttributeCache = new();
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

    private readonly record struct AttributeCacheKey(MemberInfo Member, Type AttributeType, bool Inherit);
}
