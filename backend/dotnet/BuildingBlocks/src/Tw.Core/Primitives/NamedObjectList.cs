namespace Tw.Core.Primitives;

/// <summary>
/// Provides a mutable list for named primitive descriptors.
/// </summary>
/// <typeparam name="T">The named object type stored in the list.</typeparam>
public class NamedObjectList<T> : List<T>
    where T : NamedObject;
