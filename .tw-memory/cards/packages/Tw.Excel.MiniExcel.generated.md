# Package: Tw.Excel.MiniExcel

标识：Tw.Excel.MiniExcel / backend/dotnet/BuildingBlocks/src/Excel/Tw.Excel.MiniExcel / platform-team
职责：基于 MiniExcel 与 OpenXML 的 Excel 模板导出适配实现。

适用范围：
- MiniExcel 流式写入适配
- OpenXML 模板后处理
- 多级表头和数据验证支持

不适用范围：
- HTTP 文件传输
- 业务模板管理
- 数据库存储

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: Tw.Excel, MiniExcel, DocumentFormat.OpenXml

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Excel.MiniExcel
