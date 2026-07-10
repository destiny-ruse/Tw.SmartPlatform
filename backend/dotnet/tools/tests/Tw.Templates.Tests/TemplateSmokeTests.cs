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
        var root = Path.Combine(FindToolRoot(), "Tw.Templates", "content", "service");
        var files = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories);
        var text = string.Join(Environment.NewLine, files.Select(File.ReadAllText));

        text.Should().NotContain("Tw.Infrastructure");
        text.Should().NotContain("Tw.UnitOfWork");
        text.Should().NotContain("Tw.Data.Abstractions");
        text.Should().NotContain("MassTransit");
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
}
