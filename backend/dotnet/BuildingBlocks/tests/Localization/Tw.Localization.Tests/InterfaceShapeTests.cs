using AwesomeAssertions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖InterfaceShape的核心行为和边界条件
/// </summary>
public class InterfaceShapeTests
{
    /// <summary>
    /// 本地化公开接口统一位于本地化功能命名空间
    /// </summary>
    [Fact]
    public void PublicInterfaces_LiveInLocalizationNamespace()
    {
        typeof(ITextLocalizer).Namespace.Should().Be("Tw.Localization");
        typeof(ITextResourceContributor).Namespace.Should().Be("Tw.Localization");
        typeof(IDynamicTextStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationService).Namespace.Should().Be("Tw.Localization");
        typeof(IStaticTextSnapshot).Namespace.Should().Be("Tw.Localization");
    }

    /// <summary>
    /// 验证顶层本地化查询操作的接口与实现类型均要求调用方显式提供取消令牌
    /// </summary>
    [Fact]
    public void TopLevelLocalizationOperations_RequireExplicitCancellationTokens()
    {
        AssertCancellationTokenHasNoDefaultValue(typeof(ITextLocalizer), nameof(ITextLocalizer.GetAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(ITextLocalizer), nameof(ITextLocalizer.GetAllAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(IEntityTranslationService), nameof(IEntityTranslationService.GetFieldAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(IEntityTranslationService), nameof(IEntityTranslationService.GetFieldsAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(TextLocalizer), nameof(TextLocalizer.GetAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(TextLocalizer), nameof(TextLocalizer.GetAllAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(EntityTranslationService), nameof(EntityTranslationService.GetFieldAsync));
        AssertCancellationTokenHasNoDefaultValue(typeof(EntityTranslationService), nameof(EntityTranslationService.GetFieldsAsync));
    }

    /// <summary>
    /// 断言指定服务操作的取消令牌参数没有默认值
    /// </summary>
    /// <param name="serviceType">包含待验证操作的服务类型</param>
    /// <param name="methodName">待验证操作的方法名称</param>
    private static void AssertCancellationTokenHasNoDefaultValue(Type serviceType, string methodName)
    {
        var method = serviceType.GetMethod(methodName);
        method.Should().NotBeNull();

        var cancellationToken = method!
            .GetParameters()
            .Single(parameter => parameter.ParameterType == typeof(CancellationToken));

        cancellationToken.HasDefaultValue.Should().BeFalse(
            $"{serviceType.Name}.{methodName} 必须要求调用方显式提供 CancellationToken");
    }
}
