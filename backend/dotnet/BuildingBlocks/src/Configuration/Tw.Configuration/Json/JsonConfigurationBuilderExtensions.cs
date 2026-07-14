namespace Tw.Configuration.Json;

/// <summary>
/// 提供 JSON 配置清单创建入口
/// </summary>
public static class JsonConfigurationBuilderExtensions
{
    /// <summary>
    /// 按调用方指定顺序创建 JSON 配置文件清单
    /// </summary>
    /// <param name="files">需要纳入配置加载流程的文件路径</param>
    /// <returns>保留输入顺序的 JSON 配置清单</returns>
    /// <exception cref="ArgumentNullException">files 为 null 时抛出</exception>
    public static JsonConfigurationManifest CreateManifest(params string[] files)
    {
        return new JsonConfigurationManifest(files);
    }
}
