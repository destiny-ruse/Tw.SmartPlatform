# Package: Tw.TextTemplating

标识：Tw.TextTemplating / backend/dotnet/BuildingBlocks/src/TextTemplating/Tw.TextTemplating / platform-team
职责：文本模板渲染请求、结果、诊断和渲染器抽象。

适用范围：
- 模板来源类型
- 模板渲染请求
- 模板渲染结果
- 模板渲染器接口

不适用范围：
- 具体模板引擎实现
- 文件系统访问策略实现
- 模板管理后台

依赖边界：
- forbid: Microsoft.AspNetCore.*, Scriban, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.TextTemplating
