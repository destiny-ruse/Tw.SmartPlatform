# Tw.TextTemplating.Scriban

`Tw.TextTemplating.Scriban` 使用 Scriban 实现 `ITemplateRenderer`，并通过 `TemplateFileAccessPolicy` 约束模板文件访问边界。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须完成语法、诊断、模型访问、超时与取消、文件根目录逃逸和不可信模板安全验证。

## 边界

- 持久化模板语法属于数据契约，不得透明切换引擎
- 任意文件系统访问和业务模板管理不属于本包
- Scriban 类型不得进入 `Tw.TextTemplating` 公共契约
