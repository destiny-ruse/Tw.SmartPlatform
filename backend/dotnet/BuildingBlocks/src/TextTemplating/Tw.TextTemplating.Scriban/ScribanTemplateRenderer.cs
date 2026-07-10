using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;
using Tw.TextTemplating;

namespace Tw.TextTemplating.Scriban;

/// <summary>
/// 基于 Scriban 的文本模板渲染器
/// </summary>
public sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    /// <inheritdoc />
    public async Task<TemplateRenderResult> RenderAsync(
        TemplateRenderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.SourceKind != TemplateSourceKind.String)
        {
            return TemplateRenderResult.Failed(new TemplateDiagnostic(
                "TEMPLATE:UNSUPPORTED_SOURCE",
                "当前渲染器只支持字符串模板",
                null,
                null,
                null));
        }

        var template = Template.Parse(request.Source);
        if (template.HasErrors)
        {
            return TemplateRenderResult.Failed(template.Messages
                .Select(message => new TemplateDiagnostic(
                    "TEMPLATE:PARSE",
                    message.ToString(),
                    null,
                    null,
                    null))
                .ToArray());
        }

        var context = CreateContext(request.Variables);

        try
        {
            var content = await template.RenderAsync(context).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return TemplateRenderResult.Succeeded(content);
        }
        catch (ScriptRuntimeException exception)
        {
            return TemplateRenderResult.Failed(new TemplateDiagnostic(
                "TEMPLATE:RENDER",
                exception.Message,
                null,
                null,
                null));
        }
    }

    /// <summary>
    /// 创建上下文测试对象
    /// </summary>
    /// <param name="variables">用于提供variables</param>
    /// <returns>方法计算得到的文本值</returns>
    private static TemplateContext CreateContext(IReadOnlyDictionary<string, object?> variables)
    {
        var scriptObject = new ScriptObject();
        foreach (var variable in variables)
        {
            scriptObject.TrySetValue(null!, default, variable.Key, variable.Value, true);
        }

        var context = new TemplateContext
        {
            EnableRelaxedMemberAccess = false,
            MemberFilter = _ => false,
            MemberRenamer = member => member.Name,
            StrictVariables = true,
            TemplateLoader = null,
        };
        context.PushGlobal(scriptObject);
        return context;
    }
}
