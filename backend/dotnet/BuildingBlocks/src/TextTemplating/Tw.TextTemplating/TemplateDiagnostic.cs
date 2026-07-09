namespace Tw.TextTemplating;

/// <summary>
/// 模板渲染诊断信息
/// </summary>
/// <param name="Code">诊断编码</param>
/// <param name="Message">诊断消息</param>
/// <param name="MemberName">关联成员名</param>
/// <param name="Line">行号</param>
/// <param name="Column">列号</param>
public sealed record TemplateDiagnostic(
    string Code,
    string Message,
    string? MemberName,
    int? Line,
    int? Column);
