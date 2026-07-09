namespace Tw.TextTemplating;

/// <summary>
/// 文本模板渲染请求
/// </summary>
/// <param name="SourceKind">模板来源类型</param>
/// <param name="Source">模板来源内容或定位符</param>
/// <param name="Variables">模板变量</param>
public sealed record TemplateRenderRequest(
    TemplateSourceKind SourceKind,
    string Source,
    IReadOnlyDictionary<string, object?> Variables);
