using System.Reflection;
using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 验证公开类型图不泄露 HTTP、DI 或第三方韧性 provider
/// </summary>
public sealed class ResiliencePublicApiBoundaryTests
{
    /// <summary>
    /// 递归检查公开类型的继承、接口、签名、泛型参数与约束
    /// </summary>
    [Fact]
    public void PublicApiGraph_DoesNotExposeHttpOrProviderTypes()
    {
        var forbiddenTypes = GetPublicApiTypeGraph()
            .Where(IsForbiddenBoundaryType)
            .Select(type => type.FullName ?? type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        forbiddenTypes.Should().BeEmpty();
    }

    /// <summary>
    /// 生产程序集元数据不直接引用已删除的第三方 provider 程序集
    /// </summary>
    [Fact]
    public void AssemblyMetadata_DoesNotReferenceHttpResilienceProviders()
    {
        var referencedAssemblyNames = typeof(ResiliencePolicyBuilder).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name);

        referencedAssemblyNames.Should().NotContain("Polly.Core");
        referencedAssemblyNames.Should().NotContain("Microsoft.Extensions.Http.Resilience");
    }

    /// <summary>
    /// 从所有导出类型递归收集公开 API 可到达的类型形状
    /// </summary>
    /// <returns>公开 API 直接或间接暴露的类型集合</returns>
    private static IReadOnlySet<Type> GetPublicApiTypeGraph()
    {
        var targetAssembly = typeof(ResiliencePolicyBuilder).Assembly;
        var pendingTypes = new Stack<Type>(targetAssembly.GetExportedTypes());
        var discoveredTypes = new HashSet<Type>();

        while (pendingTypes.TryPop(out var currentType))
        {
            if (!discoveredTypes.Add(currentType))
            {
                continue;
            }

            EnqueueTypeShape(currentType, pendingTypes);
            if (currentType.Assembly != targetAssembly || !currentType.IsVisible)
            {
                continue;
            }

            foreach (var exposedType in GetDeclaredPublicSurfaceTypes(currentType))
            {
                pendingTypes.Push(exposedType);
            }
        }

        return discoveredTypes;
    }

    /// <summary>
    /// 收集当前公开类型声明自身暴露的继承与成员签名类型
    /// </summary>
    /// <param name="type">需要检查的导出生产类型</param>
    /// <returns>该类型声明直接暴露的类型</returns>
    private static IEnumerable<Type> GetDeclaredPublicSurfaceTypes(Type type)
    {
        const BindingFlags publicDeclared = BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly;

        if (type.BaseType is not null)
        {
            yield return type.BaseType;
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            yield return interfaceType;
        }

        foreach (var constructor in type.GetConstructors(publicDeclared))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return parameter.ParameterType;
            }
        }

        foreach (var method in type.GetMethods(publicDeclared))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
            {
                yield return parameter.ParameterType;
            }

            foreach (var genericArgument in method.GetGenericArguments())
            {
                yield return genericArgument;
            }
        }

        foreach (var property in type.GetProperties(publicDeclared))
        {
            yield return property.PropertyType;
            foreach (var indexParameter in property.GetIndexParameters())
            {
                yield return indexParameter.ParameterType;
            }
        }

        foreach (var field in type.GetFields(publicDeclared))
        {
            yield return field.FieldType;
        }

        foreach (var eventInfo in type.GetEvents(publicDeclared))
        {
            if (eventInfo.EventHandlerType is not null)
            {
                yield return eventInfo.EventHandlerType;
            }
        }
    }

    /// <summary>
    /// 将数组元素、泛型实参与泛型约束加入待检查类型图
    /// </summary>
    /// <param name="type">需要展开形状的类型</param>
    /// <param name="pendingTypes">等待递归检查的类型栈</param>
    private static void EnqueueTypeShape(Type type, Stack<Type> pendingTypes)
    {
        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            pendingTypes.Push(elementType);
        }

        foreach (var genericArgument in type.GetGenericArguments())
        {
            pendingTypes.Push(genericArgument);
        }

        if (!type.IsGenericParameter)
        {
            return;
        }

        foreach (var constraint in type.GetGenericParameterConstraints())
        {
            pendingTypes.Push(constraint);
        }
    }

    /// <summary>
    /// 判断类型是否属于禁止泄露的 HTTP、DI 或第三方 provider 边界
    /// </summary>
    /// <param name="type">公开 API 图中的候选类型</param>
    /// <returns>类型属于禁止边界时返回 <see langword="true"/></returns>
    private static bool IsForbiddenBoundaryType(Type type)
    {
        var typeNamespace = type.Namespace ?? string.Empty;
        var fullName = type.FullName ?? string.Empty;

        return typeNamespace.StartsWith("System.Net.Http", StringComparison.Ordinal)
            || typeNamespace.StartsWith("Polly", StringComparison.Ordinal)
            || typeNamespace.StartsWith("Microsoft.Extensions.Http", StringComparison.Ordinal)
            || fullName is "Microsoft.Extensions.DependencyInjection.IServiceCollection"
                or "Microsoft.Extensions.DependencyInjection.IHttpClientBuilder";
    }
}
