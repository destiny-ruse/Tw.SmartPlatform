# Package: Tw.Excel

标识：Tw.Excel / backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel / platform-team
职责：Excel 导入导出契约、模板定义、导入错误和公式注入防护基础能力。

适用范围：
- Excel 列定义
- Excel 模板定义
- 导入校验错误
- 公式注入防护

不适用范围：
- 具体 Excel 引擎实现
- HTTP 文件上传下载
- 业务模板管理

依赖边界：
- forbid: Microsoft.AspNetCore.*, MiniExcel, DocumentFormat.OpenXml
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Excel
