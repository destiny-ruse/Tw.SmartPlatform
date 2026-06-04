namespace Tw.Localization;

/// <summary>
/// 标识一条本地化文本的来源渠道
/// </summary>
public enum LocalizedTextSource
{
    /// <summary>
    /// 文本来自静态 JSON 资源文件
    /// </summary>
    StaticJson = 0,

    /// <summary>
    /// 文本来自动态数据源（如数据库）
    /// </summary>
    Dynamic = 1,

    /// <summary>
    /// 文本来自父文化回退，例如 "zh" 是 "zh-Hans" 的父文化
    /// </summary>
    ParentCulture = 2,

    /// <summary>
    /// 文本来自系统默认文化回退
    /// </summary>
    DefaultCulture = 3,

    /// <summary>
    /// 文本来自基础资源（通常为中性文化资源）
    /// </summary>
    BaseResource = 4,

    /// <summary>
    /// 在所有文化和来源中均未找到对应的文本
    /// </summary>
    NotFound = 5,
}
