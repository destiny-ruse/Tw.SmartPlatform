namespace Tw.Configuration.Json;

/// <summary>表示 JsonConfigurationBuilderExtensions 类型</summary>
public static class JsonConfigurationBuilderExtensions
{
    /// <summary>执行 CreateManifest 操作</summary>
    /// <param name="files">files 参数</param>
    /// <returns>CreateManifest 的执行结果</returns>
    public static JsonConfigurationManifest CreateManifest(params string[] files)
    {
        return new JsonConfigurationManifest(files);
    }
}
