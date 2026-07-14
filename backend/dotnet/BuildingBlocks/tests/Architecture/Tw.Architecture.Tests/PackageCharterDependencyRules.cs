using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Tw.Architecture.Tests;

/// <summary>
/// 从包章程的 YAML 节点图读取依赖白名单
/// </summary>
internal static class PackageCharterDependencyRules
{
    /// <summary>
    /// 判断 dependency_rules.allow 是否包含指定依赖身份
    /// </summary>
    /// <param name="charterPath">待解析的包章程路径</param>
    /// <param name="dependencyName">需要匹配的 canonical 依赖名</param>
    /// <returns>白名单包含目标依赖时返回 <see langword="true"/></returns>
    /// <exception cref="InvalidDataException">YAML 语法错误或目标节点类型不符合章程结构时抛出</exception>
    internal static bool AllowsDependency(string charterPath, string dependencyName)
    {
        return ReadAllowedDependencies(charterPath, requireAllowNode: false).Any(dependency => string.Equals(
            dependency,
            dependencyName,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 读取 dependency_rules.allow 中声明的全部依赖身份
    /// </summary>
    /// <param name="charterPath">待解析的包章程路径</param>
    /// <returns>按 YAML 顺序返回的依赖身份集合；显式空序列返回空集合</returns>
    /// <exception cref="InvalidDataException">YAML 语法错误、必需节点缺失或目标节点类型不符合章程结构时抛出</exception>
    internal static IReadOnlyList<string> ReadAllowedDependencies(string charterPath)
    {
        return ReadAllowedDependencies(charterPath, requireAllowNode: true);
    }

    /// <summary>
    /// 按调用方结构要求读取 dependency_rules.allow 依赖身份
    /// </summary>
    /// <param name="charterPath">待解析的包章程路径</param>
    /// <param name="requireAllowNode">是否要求 dependency_rules 与 allow 节点显式存在</param>
    /// <returns>按 YAML 顺序返回的依赖身份集合</returns>
    /// <exception cref="InvalidDataException">YAML 语法错误、节点缺失或节点类型不符合章程结构时抛出</exception>
    private static IReadOnlyList<string> ReadAllowedDependencies(
        string charterPath,
        bool requireAllowNode)
    {
        try
        {
            using var reader = File.OpenText(charterPath);
            var yaml = new YamlStream();
            yaml.Load(reader);

            if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw InvalidStructure(charterPath, "根节点必须是单一 mapping");
            }

            var dependencyRulesNode = FindChild(root, "dependency_rules");
            if (dependencyRulesNode is null)
            {
                if (requireAllowNode)
                {
                    throw InvalidStructure(charterPath, "dependency_rules 必须存在");
                }

                return [];
            }

            if (dependencyRulesNode is not YamlMappingNode dependencyRules)
            {
                throw InvalidStructure(charterPath, "dependency_rules 必须是 mapping");
            }

            var allowNode = FindChild(dependencyRules, "allow");
            if (allowNode is null)
            {
                if (requireAllowNode)
                {
                    throw InvalidStructure(charterPath, "dependency_rules.allow 必须存在");
                }

                return [];
            }

            if (allowNode is not YamlSequenceNode allowedDependencies)
            {
                throw InvalidStructure(charterPath, "dependency_rules.allow 必须是 sequence");
            }

            return allowedDependencies.Children
                .Select(node => node is YamlScalarNode dependency && dependency.Value is not null
                    ? dependency.Value
                    : throw InvalidStructure(charterPath, "dependency_rules.allow 只能包含字符串依赖名"))
                .ToArray();
        }
        catch (YamlException exception)
        {
            throw new InvalidDataException($"无法解析包章程 YAML：{charterPath}", exception);
        }
    }

    /// <summary>
    /// 从 YAML mapping 中按精确键名读取子节点
    /// </summary>
    /// <param name="mapping">包含目标键的 YAML mapping</param>
    /// <param name="key">章程 schema 定义的键名</param>
    /// <returns>键存在时返回对应节点，否则返回 <see langword="null"/></returns>
    private static YamlNode? FindChild(YamlMappingNode mapping, string key)
    {
        return mapping.Children
            .Where(pair => pair.Key is YamlScalarNode scalar
                && string.Equals(scalar.Value, key, StringComparison.Ordinal))
            .Select(pair => pair.Value)
            .FirstOrDefault();
    }

    /// <summary>
    /// 创建包含章程路径与结构原因的解析异常
    /// </summary>
    /// <param name="charterPath">无法按 schema 读取的包章程路径</param>
    /// <param name="reason">不符合 YAML 节点结构的具体原因</param>
    /// <returns>可直接传播给架构测试的诊断异常</returns>
    private static InvalidDataException InvalidStructure(string charterPath, string reason)
    {
        return new InvalidDataException($"包章程 YAML 结构无效：{charterPath}；{reason}");
    }
}
