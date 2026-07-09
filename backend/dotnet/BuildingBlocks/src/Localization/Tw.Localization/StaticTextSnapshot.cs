using Tw.Localization.Json;

namespace Tw.Localization;

/// <summary>
/// 基于静态 JSON 资源的文本快照，实现 <see cref="IStaticTextSnapshot"/>，
/// 提供同步的高性能文本查找，适用于 <c>IStringLocalizer</c> 等性能敏感路径
/// </summary>
public sealed class StaticTextSnapshot : IStaticTextSnapshot
{
    // 索引：(resourceName, cultureName) → (key → value)；
    // 同一 (resource, culture) 的多个资源文件在构造时合并，后加载的条目覆盖先加载的
    private readonly Dictionary<(string ResourceName, string CultureName), Dictionary<string, string>> _index;

    /// <summary>
    /// 初始化 <see cref="StaticTextSnapshot"/> 类的新实例
    /// </summary>
    /// <param name="resources">已解析的 JSON 文本资源列表；同一 (资源名, 文化) 的多个条目将按输入顺序合并</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="resources"/> 为 <see langword="null"/> 时抛出</exception>
    public StaticTextSnapshot(IEnumerable<JsonTextResource> resources)
    {
        Check.NotNull(resources);

        // 构建索引时使用 OrdinalIgnoreCase 作为 (resourceName, cultureName) 的比较策略
        _index = new Dictionary<(string, string), Dictionary<string, string>>(
            ResourceCultureComparer.Instance);

        foreach (var resource in resources)
        {
            var key = (resource.ResourceName, resource.CultureName);

            if (!_index.TryGetValue(key, out var bucket))
            {
                bucket = new Dictionary<string, string>(StringComparer.Ordinal);
                _index[key] = bucket;
            }

            // 同一 (resource, culture) 的多个文件按输入顺序合并，后加载值覆盖先加载值
            foreach (var (textKey, textValue) in resource.Texts)
            {
                bucket[textKey] = textValue;
            }
        }
    }

    /// <inheritdoc />
    public LocalizedText? Find(
        string resourceName,
        string name,
        IReadOnlyList<string> candidateCultureNames)
    {
        // 按高优先级到低优先级顺序查找，首次命中即返回
        foreach (var culture in candidateCultureNames)
        {
            if (_index.TryGetValue((resourceName, culture), out var bucket) &&
                bucket.TryGetValue(name, out var value))
            {
                return new LocalizedText(
                    resourceName,
                    name,
                    value,
                    culture,
                    false,
                    LocalizedTextSource.StaticJson);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> GetAll(
        string resourceName,
        IReadOnlyList<string> candidateCultureNames)
    {
        var result = new Dictionary<string, LocalizedText>(StringComparer.Ordinal);

        // 以低优先级（后备文化）在前、高优先级（当前文化）在后的顺序填充，
        // 确保高优先级的条目最终覆盖低优先级的条目
        for (var i = candidateCultureNames.Count - 1; i >= 0; i--)
        {
            var culture = candidateCultureNames[i];

            if (!_index.TryGetValue((resourceName, culture), out var bucket))
            {
                continue;
            }

            foreach (var (key, value) in bucket)
            {
                result[key] = new LocalizedText(
                    resourceName,
                    key,
                    value,
                    culture,
                    false,
                    LocalizedTextSource.StaticJson);
            }
        }

        return result.AsReadOnly();
    }

    // 用于 Dictionary 键的比较器：资源名和文化名均使用 OrdinalIgnoreCase 比较
    private sealed class ResourceCultureComparer : IEqualityComparer<(string ResourceName, string CultureName)>
    {
        public static readonly ResourceCultureComparer Instance = new();

        public bool Equals((string ResourceName, string CultureName) x, (string ResourceName, string CultureName) y) =>
            string.Equals(x.ResourceName, y.ResourceName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.CultureName, y.CultureName, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string ResourceName, string CultureName) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ResourceName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CultureName));
    }
}
