namespace Tw.TextTemplating;

/// <summary>
/// 模板来源类型
/// </summary>
public enum TemplateSourceKind
{
    /// <summary>
    /// 字符串模板
    /// </summary>
    String,

    /// <summary>
    /// 文件模板
    /// </summary>
    File,

    /// <summary>
    /// 嵌入资源模板
    /// </summary>
    EmbeddedResource,

    /// <summary>
    /// 配置模板
    /// </summary>
    Configuration,
}
