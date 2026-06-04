namespace Tw.Localization;

/// <summary>
/// 指定当本地化键在资源中不存在时的回退行为
/// </summary>
public enum MissingTextBehavior
{
    /// <summary>
    /// 将键本身作为文本值返回
    /// </summary>
    ReturnKey = 0,

    /// <summary>
    /// 返回空字符串
    /// </summary>
    ReturnEmptyString = 1,

    /// <summary>
    /// 将键本身作为文本值返回，并同时记录一条诊断信息
    /// </summary>
    ReturnKeyAndRecordDiagnostic = 2,
}
