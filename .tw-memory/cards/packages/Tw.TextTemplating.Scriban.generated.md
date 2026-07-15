# Package: Tw.TextTemplating.Scriban

标识：Tw.TextTemplating.Scriban / backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating.Scriban / platform-team
职责：基于 Scriban 的文本模板渲染适配器和模板文件访问边界。

适用范围：
- Scriban 渲染器适配
- 模板文件根目录访问策略
- 模板安全执行边界

不适用范围：
- 模板管理后台
- 业务模板内容
- 任意文件系统访问

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: Tw.TextTemplating, Scriban

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.TextTemplating.Scriban
