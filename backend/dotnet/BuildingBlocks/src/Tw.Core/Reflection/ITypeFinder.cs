using System.Reflection;

namespace Tw.Core.Reflection;

/// <summary>
/// Finds runtime types from a configured set of assemblies.
/// </summary>
public interface ITypeFinder
{
    /// <summary>
    /// Gets the assemblies that are searched for types.
    /// </summary>
    IReadOnlyList<Assembly> Assemblies { get; }

    /// <summary>
    /// Finds loadable types from the configured assemblies.
    /// </summary>
    /// <returns>The discovered types in assembly traversal order.</returns>
    IReadOnlyList<Type> FindTypes();

    /// <summary>
    /// Finds concrete types assignable to the supplied base type.
    /// </summary>
    /// <param name="baseType">The base type or interface that discovered types must implement.</param>
    /// <returns>The matching concrete types in discovery order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseType"/> is <see langword="null"/>.</exception>
    IReadOnlyList<Type> FindTypes(Type baseType);

    /// <summary>
    /// Finds concrete types assignable to the supplied base type parameter.
    /// </summary>
    /// <typeparam name="TBaseType">The base type or interface that discovered types must implement.</typeparam>
    /// <returns>The matching concrete types in discovery order.</returns>
    IReadOnlyList<Type> FindTypes<TBaseType>();
}
