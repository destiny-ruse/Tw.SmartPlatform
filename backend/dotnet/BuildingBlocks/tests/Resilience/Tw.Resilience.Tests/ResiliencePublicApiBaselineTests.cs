using System.Reflection;
using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 锁定 Tw.Resilience 允许导出的类型与业务成员基线
/// </summary>
public sealed class ResiliencePublicApiBaselineTests
{
    /// <summary>
    /// record 编译器生成且不表达新增业务入口的方法名称
    /// </summary>
    private static readonly IReadOnlySet<string> RecordInfrastructureMethodNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "ToString",
            "GetHashCode",
            "Equals",
            "Deconstruct",
            "<Clone>$"
        };

    /// <summary>
    /// Task 10 允许的全部导出类型、构造入口、属性、枚举值与业务方法
    /// </summary>
    private static readonly IReadOnlySet<string> ExpectedPublicApi =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "type:Tw.Resilience.OperationKind",
            "type:Tw.Resilience.ResiliencePolicy",
            "type:Tw.Resilience.ResiliencePolicyBuilder",
            "type:Tw.Resilience.ResiliencePolicyDescriptor",
            "constructor:Tw.Resilience.ResiliencePolicyDescriptor(System.String,Tw.Resilience.OperationKind,System.TimeSpan,System.Int32,System.Boolean,System.Boolean,System.Boolean,System.Boolean)",
            "field:Tw.Resilience.OperationKind.IdempotentWrite:Tw.Resilience.OperationKind",
            "field:Tw.Resilience.OperationKind.NonIdempotentWrite:Tw.Resilience.OperationKind",
            "field:Tw.Resilience.OperationKind.Read:Tw.Resilience.OperationKind",
            "method:Tw.Resilience.ResiliencePolicyBuilder.Build(Tw.Resilience.ResiliencePolicyDescriptor):Tw.Resilience.ResiliencePolicy",
            "method:Tw.Resilience.ResiliencePolicyDescriptor.ForHttp(System.String,Tw.Resilience.OperationKind,System.TimeSpan):Tw.Resilience.ResiliencePolicyDescriptor",
            "property:Tw.Resilience.ResiliencePolicy.CircuitBreakerEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicy.ConcurrencyLimiterEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicy.FallbackEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicy.OperationKind:Tw.Resilience.OperationKind",
            "property:Tw.Resilience.ResiliencePolicy.OperationName:System.String",
            "property:Tw.Resilience.ResiliencePolicy.RateLimiterEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicy.RetryCount:System.Int32",
            "property:Tw.Resilience.ResiliencePolicy.RetryEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicy.Timeout:System.TimeSpan",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.CircuitBreakerEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.ConcurrencyLimiterEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.FallbackEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.OperationKind:Tw.Resilience.OperationKind",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.OperationName:System.String",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.RateLimiterEnabled:System.Boolean",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.RetryCount:System.Int32",
            "property:Tw.Resilience.ResiliencePolicyDescriptor.Timeout:System.TimeSpan"
        };

    /// <summary>
    /// 导出业务 API 必须与 Task 10 批准基线完全一致
    /// </summary>
    [Fact]
    public void ExportedApi_MatchesApprovedBaseline()
    {
        ReadPublicApi().Should().BeEquivalentTo(ExpectedPublicApi);
    }

    /// <summary>
    /// 已删除的 HTTP 韧性注册入口不得作为占位方法恢复
    /// </summary>
    [Fact]
    public void HttpResilienceRegistrationEntrypoints_AreNotExported()
    {
        var exportedMethodNames = typeof(ResiliencePolicyBuilder).Assembly
            .GetExportedTypes()
            .SelectMany(type => type.GetMethods(PublicDeclaredMembers))
            .Select(method => method.Name);

        exportedMethodNames.Should().NotContain("AddTwHttpResilience");
        exportedMethodNames.Should().NotContain("AddHttpResilience");
    }

    /// <summary>
    /// 公开声明成员的反射选择范围
    /// </summary>
    private const BindingFlags PublicDeclaredMembers = BindingFlags.Public
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    /// <summary>
    /// 从生产程序集读取稳定的公开业务 API 标识
    /// </summary>
    /// <returns>可与批准基线进行集合比较的成员标识</returns>
    private static IReadOnlySet<string> ReadPublicApi()
    {
        var apiMembers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in typeof(ResiliencePolicyBuilder).Assembly.GetExportedTypes())
        {
            var declaringTypeName = TypeName(type);
            apiMembers.Add($"type:{declaringTypeName}");

            foreach (var constructor in type.GetConstructors(PublicDeclaredMembers))
            {
                apiMembers.Add(
                    $"constructor:{declaringTypeName}({ParameterTypeNames(constructor.GetParameters())})");
            }

            foreach (var field in type.GetFields(PublicDeclaredMembers).Where(field => !field.IsSpecialName))
            {
                apiMembers.Add($"field:{declaringTypeName}.{field.Name}:{TypeName(field.FieldType)}");
            }

            foreach (var property in type.GetProperties(PublicDeclaredMembers))
            {
                apiMembers.Add($"property:{declaringTypeName}.{property.Name}:{TypeName(property.PropertyType)}");
            }

            foreach (var method in type.GetMethods(PublicDeclaredMembers)
                         .Where(method => !method.IsSpecialName
                             && !RecordInfrastructureMethodNames.Contains(method.Name)))
            {
                apiMembers.Add(
                    $"method:{declaringTypeName}.{method.Name}({ParameterTypeNames(method.GetParameters())}):{TypeName(method.ReturnType)}");
            }
        }

        return apiMembers;
    }

    /// <summary>
    /// 将反射参数列表转换为稳定类型名列表
    /// </summary>
    /// <param name="parameters">构造函数或方法的公开参数</param>
    /// <returns>以逗号分隔且不包含程序集版本的类型名</returns>
    private static string ParameterTypeNames(IEnumerable<ParameterInfo> parameters)
    {
        return string.Join(',', parameters.Select(parameter => TypeName(parameter.ParameterType)));
    }

    /// <summary>
    /// 获取不包含程序集版本的稳定反射类型名
    /// </summary>
    /// <param name="type">公开 API 暴露的类型</param>
    /// <returns>类型完整名称，泛型参数等无完整名称的形状返回反射名称</returns>
    private static string TypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }
}
