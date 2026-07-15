# Tw.TextTemplating

`Tw.TextTemplating` 定义文本模板渲染的 provider-neutral 契约。

## 公开能力

- `ITemplateRenderer`
- `TemplateRenderRequest` 与 `TemplateRenderResult`
- `TemplateDiagnostic`
- `TemplateSourceKind`

## 稳定性与边界

本包处于 `experimental` 阶段。模板管理后台、业务模板内容和具体语法引擎不属于本包；稳定前必须冻结输入模型、诊断、取消、文件来源和失败语义。
