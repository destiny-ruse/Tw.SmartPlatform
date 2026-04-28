namespace Tw.Core.Reflection;

/// <summary>
/// Provides dependency-free convenience methods for <see cref="ITypeFinder"/>.
/// </summary>
public static class TypeFinderExtensions
{
    /// <summary>
    /// Finds all concrete types from the configured type finder.
    /// </summary>
    /// <param name="typeFinder">The type finder to query.</param>
    /// <returns>The concrete types in discovery order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeFinder"/> is <see langword="null"/>.</exception>
    public static IEnumerable<Type> FindConcreteTypes(this ITypeFinder typeFinder)
    {
        return Check.NotNull(typeFinder)
            .FindTypes()
            .Where(IsConcrete);
    }

    /// <summary>
    /// Finds concrete types assignable to the supplied base type parameter.
    /// </summary>
    /// <typeparam name="TBaseType">The base type or interface that discovered types must implement.</typeparam>
    /// <param name="typeFinder">The type finder to query.</param>
    /// <returns>The matching concrete types in discovery order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeFinder"/> is <see langword="null"/>.</exception>
    public static IEnumerable<Type> FindConcreteTypesAssignableTo<TBaseType>(this ITypeFinder typeFinder)
    {
        return FindConcreteTypesAssignableTo(typeFinder, typeof(TBaseType));
    }

    /// <summary>
    /// Finds concrete types assignable to the supplied base type.
    /// </summary>
    /// <param name="typeFinder">The type finder to query.</param>
    /// <param name="baseType">The base type or interface that discovered types must implement.</param>
    /// <returns>The matching concrete types in discovery order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeFinder"/> or <paramref name="baseType"/> is <see langword="null"/>.</exception>
    public static IEnumerable<Type> FindConcreteTypesAssignableTo(this ITypeFinder typeFinder, Type baseType)
    {
        Check.NotNull(baseType);

        return Check.NotNull(typeFinder).FindTypes(baseType);
    }

    private static bool IsConcrete(Type type)
    {
        return !type.IsAbstract && !type.IsInterface;
    }
}
