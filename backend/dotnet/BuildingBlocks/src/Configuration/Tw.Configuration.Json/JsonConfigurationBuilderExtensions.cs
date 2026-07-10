namespace Tw.Configuration.Json;

/// <summary>
/// 封装JSONConfiguration构建器Extensions相关的数据和行为
/// </summary>
public static class JsonConfigurationBuilderExtensions
{
    /// <summary>
    /// 创建Manifest测试对象
    /// </summary>
    /// <param name="files">用于提供files</param>
    /// <returns>方法计算得到的文本值</returns>
    public static JsonConfigurationManifest CreateManifest(params string[] files)
    {
        return new JsonConfigurationManifest(files);
    }
}
