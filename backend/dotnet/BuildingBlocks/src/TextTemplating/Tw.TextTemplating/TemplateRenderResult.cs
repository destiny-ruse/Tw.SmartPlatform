namespace Tw.TextTemplating;

/// <summary>
/// 文本模板渲染结果
/// </summary>
/// <param name="Success">渲染是否成功</param>
/// <param name="Content">渲染内容</param>
/// <param name="Diagnostics">诊断信息集合</param>
public sealed record TemplateRenderResult(
    bool Success,
    string? Content,
    IReadOnlyList<TemplateDiagnostic> Diagnostics)
{
    /// <summary>
    /// 创建成功渲染结果
    /// </summary>
    /// <param name="content">渲染内容</param>
    /// <returns>成功渲染结果</returns>
    public static TemplateRenderResult Succeeded(string content)
    {
        return new TemplateRenderResult(true, content, Array.Empty<TemplateDiagnostic>());
    }

    /// <summary>
    /// 创建失败渲染结果
    /// </summary>
    /// <param name="diagnostics">诊断信息集合</param>
    /// <returns>失败渲染结果</returns>
    public static TemplateRenderResult Failed(params TemplateDiagnostic[] diagnostics)
    {
        return new TemplateRenderResult(false, null, diagnostics);
    }
}
