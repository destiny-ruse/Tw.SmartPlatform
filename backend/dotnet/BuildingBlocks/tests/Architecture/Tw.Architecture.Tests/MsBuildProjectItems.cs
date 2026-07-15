using System.Xml.Linq;

namespace Tw.Architecture.Tests;

/// <summary>
/// 统一读取静态 MSBuild XML 中指定 item 类型及其 Include item-spec
/// </summary>
internal static class MsBuildProjectItems
{
    /// <summary>
    /// 读取目标 item，并按分号拆分和清理 Include 中的静态 item-spec
    /// </summary>
    /// <param name="projectPath">待读取的 MSBuild 项目文件</param>
    /// <param name="itemNames">需要匹配的 MSBuild item 类型名</param>
    /// <returns>保留原始 Include 与拆分 item-spec 的项目项序列</returns>
    internal static IEnumerable<MsBuildProjectItem> Read(string projectPath, params string[] itemNames)
    {
        var expectedItemNames = itemNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return XDocument.Load(projectPath)
            .Descendants()
            .Where(element => expectedItemNames.Contains(element.Name.LocalName))
            .Select(element =>
            {
                var include = element.Attributes()
                    .Where(attribute => attribute.Name.Namespace == XNamespace.None
                        && string.Equals(
                            attribute.Name.LocalName,
                            "Include",
                            StringComparison.Ordinal))
                    .Select(attribute => attribute.Value)
                    .FirstOrDefault();
                var itemSpecs = include?
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    ?? [];

                return new MsBuildProjectItem(element.Name.LocalName, include, itemSpecs);
            })
            .ToArray();
    }

    /// <summary>
    /// 判断 item-spec 是否包含需要 MSBuild evaluation 的属性、item 或 metadata 表达式
    /// </summary>
    /// <param name="itemSpec">已经从 Include 拆分的单个 item-spec</param>
    /// <returns>包含动态 MSBuild 表达式时返回 <see langword="true"/></returns>
    internal static bool ContainsExpression(string itemSpec)
    {
        return itemSpec.Contains("$(", StringComparison.Ordinal)
            || itemSpec.Contains("@(", StringComparison.Ordinal)
            || itemSpec.Contains("%(", StringComparison.Ordinal);
    }

    /// <summary>
    /// 按指定宿主分隔符转换 MSBuild item-spec
    /// </summary>
    /// <param name="itemSpec">来自 Include 的静态 item-spec</param>
    /// <param name="directorySeparator">目标文件系统目录分隔符</param>
    /// <returns>可交给 <see cref="Path"/> API 的路径文本</returns>
    internal static string NormalizeFileSystemPath(string itemSpec, char directorySeparator)
    {
        return itemSpec
            .Replace('\\', directorySeparator)
            .Replace('/', directorySeparator);
    }
}

/// <summary>
/// 表示从单个 MSBuild item 元素读取的静态 Include 信息
/// </summary>
internal sealed class MsBuildProjectItem
{
    /// <summary>
    /// 初始化 MSBuild item 的元素名、原始 Include 与拆分结果
    /// </summary>
    /// <param name="itemName">忽略 XML namespace 后的 item 类型名</param>
    /// <param name="include">Include 属性的原始值，属性缺失时为 <see langword="null"/></param>
    /// <param name="itemSpecs">按分号拆分并清理空白后的静态 item-spec</param>
    internal MsBuildProjectItem(string itemName, string? include, IReadOnlyList<string> itemSpecs)
    {
        ItemName = itemName;
        Include = include;
        ItemSpecs = itemSpecs;
    }

    /// <summary>
    /// 忽略 XML namespace 后的 item 类型名
    /// </summary>
    internal string ItemName { get; }

    /// <summary>
    /// Include 属性的原始值，属性缺失时为 <see langword="null"/>
    /// </summary>
    internal string? Include { get; }

    /// <summary>
    /// 按分号拆分并清理空白后的静态 item-spec
    /// </summary>
    internal IReadOnlyList<string> ItemSpecs { get; }
}
