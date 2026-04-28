namespace Tw.Core.Collections;

/// <summary>
/// Represents a list of types assignable to <see cref="object"/>.
/// </summary>
public interface ITypeList : ITypeList<object>;

/// <summary>
/// Represents a list of types constrained to a base type.
/// </summary>
/// <typeparam name="TBaseType">The required base type for every item.</typeparam>
public interface ITypeList<TBaseType> : IList<Type>
{
    /// <summary>
    /// Adds the specified type argument to the list.
    /// </summary>
    /// <typeparam name="T">The type to add.</typeparam>
    void Add<T>()
        where T : TBaseType;

    /// <summary>
    /// Adds the specified type argument when it is not already present.
    /// </summary>
    /// <typeparam name="T">The type to add.</typeparam>
    /// <returns><see langword="true"/> when the type was added; otherwise, <see langword="false"/>.</returns>
    bool TryAdd<T>()
        where T : TBaseType;

    /// <summary>
    /// Returns whether the specified type argument is present.
    /// </summary>
    /// <typeparam name="T">The type to locate.</typeparam>
    /// <returns><see langword="true"/> when the type is present; otherwise, <see langword="false"/>.</returns>
    bool Contains<T>()
        where T : TBaseType;

    /// <summary>
    /// Removes the specified type argument from the list.
    /// </summary>
    /// <typeparam name="T">The type to remove.</typeparam>
    /// <returns><see langword="true"/> when the type was removed; otherwise, <see langword="false"/>.</returns>
    bool Remove<T>()
        where T : TBaseType;
}
