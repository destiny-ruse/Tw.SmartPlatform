using AwesomeAssertions;
using Xunit;

namespace Tw.Localization.Tests;

/// <summary>
/// 覆盖InterfaceShape的核心行为和边界条件
/// </summary>
public class InterfaceShapeTests
{
    /// <summary>
    /// 验证PublicInterfacesLiveInTw本地化Namespace
    /// </summary>
    [Fact]
    public void PublicInterfaces_LiveInTwLocalizationNamespace()
    {
        typeof(ITextLocalizer).Namespace.Should().Be("Tw.Localization");
        typeof(ITextResourceContributor).Namespace.Should().Be("Tw.Localization");
        typeof(IDynamicTextStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationStore).Namespace.Should().Be("Tw.Localization");
        typeof(IEntityTranslationService).Namespace.Should().Be("Tw.Localization");
        typeof(IStaticTextSnapshot).Namespace.Should().Be("Tw.Localization");
    }
}
