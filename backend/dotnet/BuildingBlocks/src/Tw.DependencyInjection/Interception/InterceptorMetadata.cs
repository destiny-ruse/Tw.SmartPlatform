using System.Reflection;
using Tw.Core;

namespace Tw.DependencyInjection.Interception;

internal static class InterceptorMetadata
{
    private static readonly IReadOnlyList<InterceptorRegistration> EmptyRegistrations =
        Array.AsReadOnly(Array.Empty<InterceptorRegistration>());

    public static IReadOnlyList<InterceptorRegistration> Empty => EmptyRegistrations;

    public static IReadOnlyList<InterceptorRegistration> ToReadOnlyList(
        IEnumerable<InterceptorRegistration> registrations)
        => Array.AsReadOnly(registrations.ToArray());

    public static IReadOnlyList<InterceptorRegistration> SortLayer(
        IEnumerable<InterceptorRegistration> registrations)
        => registrations
            .GroupBy(r => (r.InterceptorType, r.Scope, r.Order))
            .Select(g => g.First())
            .OrderBy(r => r.Order)
            .ThenBy(r => r.InterceptorType.FullName ?? r.InterceptorType.Name, StringComparer.Ordinal)
            .ToArray();

    public static IgnoreRules ReadIgnoreRules(Type targetType, IEnumerable<Type> declaredTypes)
    {
        Check.NotNull(targetType);
        Check.NotNull(declaredTypes);

        var allTypes = EnumerateRelevantTypes(targetType, declaredTypes);
        var ignoredTypes = new HashSet<Type>();
        var ignoreAll = false;

        foreach (var attribute in allTypes.SelectMany(ReadIgnoreAttributes))
        {
            if (attribute.InterceptorTypes.Count == 0)
            {
                ignoreAll = true;
            }
            else
            {
                foreach (var interceptorType in attribute.InterceptorTypes)
                {
                    ignoredTypes.Add(interceptorType);
                }
            }
        }

        return new IgnoreRules(ignoreAll, ignoredTypes);
    }

    public static IEnumerable<InterceptorRegistration> ApplyIgnoreRules(
        IEnumerable<InterceptorRegistration> registrations,
        IgnoreRules rules)
    {
        foreach (var registration in registrations)
        {
            if (rules.IgnoreAll || rules.IgnoredTypes.Contains(registration.InterceptorType))
            {
                continue;
            }

            yield return registration;
        }
    }

    public static IReadOnlyList<InterceptorRegistration> ReadExplicitServiceInterceptors(
        Type implementationType,
        IEnumerable<Type> serviceTypes)
    {
        var relevantTypes = EnumerateRelevantTypes(implementationType, serviceTypes);
        return SortLayer(
            relevantTypes
                .SelectMany(ReadInterceptAttributes)
                .Select(a => new InterceptorRegistration(a.InterceptorType, InterceptorScope.Service)));
    }

    private static IEnumerable<Type> EnumerateRelevantTypes(Type targetType, IEnumerable<Type> declaredTypes)
    {
        yield return targetType;

        foreach (var interfaceType in targetType.GetInterfaces())
        {
            yield return interfaceType;
        }

        foreach (var declaredType in declaredTypes)
        {
            yield return declaredType;

            if (declaredType.IsInterface)
            {
                foreach (var interfaceType in declaredType.GetInterfaces())
                {
                    yield return interfaceType;
                }
            }
        }
    }

    private static IEnumerable<IgnoreInterceptorsAttribute> ReadIgnoreAttributes(Type type)
        => type.GetCustomAttributes<IgnoreInterceptorsAttribute>(inherit: true);

    private static IEnumerable<InterceptAttribute> ReadInterceptAttributes(Type type)
        => type.GetCustomAttributes<InterceptAttribute>(inherit: true);

    internal sealed class IgnoreRules(bool ignoreAll, HashSet<Type> ignoredTypes)
    {
        public bool IgnoreAll { get; } = ignoreAll;

        public HashSet<Type> IgnoredTypes { get; } = ignoredTypes;
    }
}
