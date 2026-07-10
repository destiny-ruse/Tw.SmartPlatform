using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Tw.Templates.Tests;

/// <summary>
/// 覆盖模板冒烟的核心行为和边界条件
/// </summary>
public sealed class TemplateSmokeTests
{
    /// <summary>
    /// 验证服务模板不引用禁止包
    /// </summary>
    [Fact]
    public void ServiceTemplate_DoesNotReferenceForbiddenPackages()
    {
        var root = Path.Combine(FindToolRoot(), "src", "Tw.Templates", "content", "service");
        var files = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);
        var text = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        text.Should().NotContain("Tw.Infrastructure");
        text.Should().NotContain("Tw.UnitOfWork");
        text.Should().NotContain("Tw.Data.Abstractions");
        text.Should().NotContain("MassTransit");
    }

    /// <summary>
    /// 验证网关模板在源码仓库内使用项目引用兜底
    /// </summary>
    [Fact]
    public void GatewayTemplate_UsesRepositoryProjectReferencesForInternalPackages()
    {
        var projectFile = Path.Combine(
            FindToolRoot(),
            "src",
            "Tw.Templates",
            "content",
            "gateway",
            "src",
            "Company.Gateway.Host",
            "Company.Gateway.Host.csproj");

        var document = XDocument.Load(projectFile);
        var projectReferences = document
            .Descendants("ProjectReference")
            .Select(reference => NormalizeProjectPath(reference.Attribute("Include")?.Value))
            .ToArray();

        projectReferences.Should().Contain([
            "../../../../../../../BuildingBlocks/src/Gateway/Tw.Gateway/Tw.Gateway.csproj",
            "../../../../../../../BuildingBlocks/src/Gateway/Tw.Gateway.Yarp/Tw.Gateway.Yarp.csproj",
            "../../../../../../../BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj",
            "../../../../../../../BuildingBlocks/src/Observability/Tw.Observability/Tw.Observability.csproj",
            "../../../../../../../BuildingBlocks/src/Configuration/Tw.Configuration/Tw.Configuration.csproj"
        ]);

        var internalPackageReferences = document
            .Descendants("PackageReference")
            .Where(reference => reference.Attribute("Include")?.Value.StartsWith("Tw.", StringComparison.Ordinal) == true)
            .ToArray();

        internalPackageReferences.Should().OnlyContain(reference => UsesPackageFallbackCondition(reference));
    }

    /// <summary>
    /// 查找工具根目录并返回匹配结果
    /// </summary>
    /// <returns>当前工具源码根目录路径</returns>
    private static string FindToolRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var tools = Path.Combine(directory.FullName, "backend", "dotnet", "tools");
            if (Directory.Exists(tools))
            {
                return tools;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate backend/dotnet/tools.");
    }

    /// <summary>
    /// 将项目文件路径转换为稳定的跨平台比较格式
    /// </summary>
    /// <param name="path">项目文件中的 Include 路径</param>
    /// <returns>使用正斜杠分隔的项目路径</returns>
    private static string NormalizeProjectPath(string? path)
    {
        return path?.Replace('\\', '/') ?? string.Empty;
    }

    /// <summary>
    /// 判断内部包引用是否只在项目引用兜底未启用时参与还原
    /// </summary>
    /// <param name="reference">内部包引用元素</param>
    /// <returns>包引用所在 ItemGroup 是否具备兜底条件</returns>
    private static bool UsesPackageFallbackCondition(XElement reference)
    {
        return reference
            .Ancestors("ItemGroup")
            .Any(group => string.Equals(
                group.Attribute("Condition")?.Value,
                "'$(UseRepositoryProjectReferences)' != 'true'",
                StringComparison.Ordinal));
    }
}
