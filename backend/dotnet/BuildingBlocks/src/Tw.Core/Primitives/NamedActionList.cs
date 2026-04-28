namespace Tw.Core.Primitives;

/// <summary>
/// Provides a mutable list for named actions.
/// </summary>
/// <typeparam name="T">The action argument type.</typeparam>
public class NamedActionList<T> : NamedObjectList<NamedAction<T>>;
