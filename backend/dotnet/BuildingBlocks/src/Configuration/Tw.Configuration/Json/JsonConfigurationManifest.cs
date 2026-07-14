namespace Tw.Configuration.Json;

/// <summary>
/// 描述需要按顺序加载的 JSON 配置文件
/// </summary>
public sealed record JsonConfigurationManifest
{
    /// <summary>
    /// 使用有序文件路径集合创建 JSON 配置清单
    /// </summary>
    /// <param name="files">按配置覆盖顺序排列的文件路径</param>
    /// <exception cref="ArgumentNullException">files 为 null 时抛出</exception>
    public JsonConfigurationManifest(IReadOnlyList<string> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        Files = files;
    }

    /// <summary>
    /// 按配置覆盖顺序排列的文件路径
    /// </summary>
    public IReadOnlyList<string> Files { get; }
}
