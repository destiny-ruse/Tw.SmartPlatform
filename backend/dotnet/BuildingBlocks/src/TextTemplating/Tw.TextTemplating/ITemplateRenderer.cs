namespace Tw.TextTemplating;

/// <summary>
/// 文本模板渲染器
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// 异步渲染文本模板
    /// </summary>
    /// <param name="request">模板渲染请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>模板渲染结果</returns>
    Task<TemplateRenderResult> RenderAsync(TemplateRenderRequest request, CancellationToken cancellationToken);
}
