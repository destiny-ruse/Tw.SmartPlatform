using System.Reflection;
using AwesomeAssertions;
using Tw.Resilience;
using Xunit;

namespace Tw.Resilience.Tests;

/// <summary>
/// 验证韧性描述、操作分类和 provider-neutral 边界
/// </summary>
public sealed class ResiliencePolicyBuilderTests
{
    /// <summary>
    /// 非幂等写操作始终禁用自动重试
    /// </summary>
    [Fact]
    public void Build_DisablesRetryForNonIdempotentWrite()
    {
        var descriptor = CreateDescriptor(OperationKind.NonIdempotentWrite, retryCount: 3);

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.RetryEnabled.Should().BeFalse();
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// 读取操作配置重试次数后启用自动重试
    /// </summary>
    [Fact]
    public void Build_EnablesRetryForReadOperation()
    {
        var descriptor = CreateDescriptor(OperationKind.Read, retryCount: 2);

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.RetryEnabled.Should().BeTrue();
    }

    /// <summary>
    /// 幂等操作的重试次数为零时禁用自动重试
    /// </summary>
    [Fact]
    public void Build_DisablesRetryWhenRetryCountIsZero()
    {
        var descriptor = CreateDescriptor(OperationKind.IdempotentWrite, retryCount: 0);

        var policy = ResiliencePolicyBuilder.Build(descriptor);

        policy.RetryEnabled.Should().BeFalse();
    }

    /// <summary>
    /// 空白操作名称无法构建可诊断的韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsBlankOperationName()
    {
        var descriptor = CreateDescriptor(OperationKind.Read, operationName: " ");

        var action = () => ResiliencePolicyBuilder.Build(descriptor);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("descriptor")
            .WithMessage("*操作名称不能为空*");
    }

    /// <summary>
    /// 非正超时时间无法构建韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsNonPositiveTimeout()
    {
        var descriptor = CreateDescriptor(OperationKind.Read, timeout: TimeSpan.Zero);

        var action = () => ResiliencePolicyBuilder.Build(descriptor);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*超时时间必须大于零*");
    }

    /// <summary>
    /// 负数重试次数无法构建韧性策略
    /// </summary>
    [Fact]
    public void Build_RejectsNegativeRetryCount()
    {
        var descriptor = CreateDescriptor(OperationKind.Read, retryCount: -1);

        var action = () => ResiliencePolicyBuilder.Build(descriptor);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*重试次数不能小于零*");
    }

    /// <summary>
    /// 未知操作分类无法绕过幂等重试边界
    /// </summary>
    [Fact]
    public void Build_RejectsUnknownOperationKind()
    {
        var descriptor = CreateDescriptor((OperationKind)999);

        var action = () => ResiliencePolicyBuilder.Build(descriptor);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("descriptor")
            .WithMessage("*操作分类不受支持*");
    }

    /// <summary>
    /// 描述工厂拒绝缺少诊断名称的操作
    /// </summary>
    [Fact]
    public void ForHttp_RejectsBlankOperationName()
    {
        var action = () => ResiliencePolicyDescriptor.ForHttp(
            " ",
            OperationKind.Read,
            TimeSpan.FromSeconds(3));

        action.Should().Throw<ArgumentException>()
            .WithParameterName("operationName")
            .WithMessage("*操作名称不能为空*");
    }

    /// <summary>
    /// 描述工厂以可诊断消息拒绝非正超时时间
    /// </summary>
    [Fact]
    public void ForHttp_RejectsNonPositiveTimeout()
    {
        var action = () => ResiliencePolicyDescriptor.ForHttp(
            "GetOrder",
            OperationKind.Read,
            TimeSpan.Zero);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeout")
            .WithMessage("*超时时间必须大于零*");
    }

    /// <summary>
    /// 公开 API 不包含 HTTP 注册入口或第三方韧性类型
    /// </summary>
    [Fact]
    public void PublicApi_DoesNotExposeHttpRegistrationOrProviderTypes()
    {
        var exportedTypes = typeof(ResiliencePolicyBuilder).Assembly.GetExportedTypes();
        var publicMethodNames = exportedTypes
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
            .Select(method => method.Name);
        var publicSignatureTypes = exportedTypes.SelectMany(GetPublicSignatureTypes);
        var providerSignatureTypes = publicSignatureTypes
            .Where(type => type.FullName?.Contains("System.Net.Http.HttpClient", StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith("Polly", StringComparison.Ordinal) == true
                || type.Namespace?.StartsWith("Microsoft.Extensions.Http.Resilience", StringComparison.Ordinal) == true);

        publicMethodNames.Should().NotContain("AddTwHttpResilience");
        publicMethodNames.Should().NotContain("AddHttpResilience");
        providerSignatureTypes.Should().BeEmpty();
    }

    /// <summary>
    /// 生产程序集不引用第三方 HTTP 韧性 provider
    /// </summary>
    [Fact]
    public void Assembly_DoesNotReferenceHttpResilienceProviders()
    {
        var referencedAssemblyNames = typeof(ResiliencePolicyBuilder).Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name);

        referencedAssemblyNames.Should().NotContain("Polly.Core");
        referencedAssemblyNames.Should().NotContain("Microsoft.Extensions.Http.Resilience");
    }

    /// <summary>
    /// 创建覆盖构建器输入边界的公司自有策略描述
    /// </summary>
    /// <param name="operationKind">决定自动重试安全性的操作分类</param>
    /// <param name="retryCount">策略允许的最大重试次数</param>
    /// <param name="operationName">用于诊断和治理的操作名称</param>
    /// <param name="timeout">单次操作允许的最长时间</param>
    /// <returns>不含第三方 provider 类型的策略描述</returns>
    private static ResiliencePolicyDescriptor CreateDescriptor(
        OperationKind operationKind,
        int retryCount = 3,
        string operationName = "GetOrder",
        TimeSpan? timeout = null)
    {
        return new ResiliencePolicyDescriptor(
            operationName,
            operationKind,
            timeout ?? TimeSpan.FromSeconds(3),
            retryCount,
            CircuitBreakerEnabled: true,
            RateLimiterEnabled: true,
            ConcurrencyLimiterEnabled: true,
            FallbackEnabled: false);
    }

    /// <summary>
    /// 收集公开类型在方法、属性、字段与事件签名中暴露的类型
    /// </summary>
    /// <param name="type">需要检查公开签名的生产类型</param>
    /// <returns>该生产类型所有公开签名直接引用的类型</returns>
    private static IEnumerable<Type> GetPublicSignatureTypes(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType)
                .Append(method.ReturnType))
            .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(property => property.PropertyType))
            .Concat(type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(field => field.FieldType))
            .Concat(type.GetEvents(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Select(eventInfo => eventInfo.EventHandlerType)
                .OfType<Type>());
    }
}
