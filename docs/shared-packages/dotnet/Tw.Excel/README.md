# Tw.Excel

`Tw.Excel` 定义 provider-neutral 的 Excel 导入导出契约、模板列定义、导入错误和公式注入防护。

## 公开能力

- `IExcelImporter` 与 `IExcelExporter`
- `ExcelTemplateDefinition` 与 `ExcelColumnDefinition`
- `ExcelImportError`
- `FormulaInjectionProtector`

## 稳定性与边界

本包处于 `experimental` 阶段。MiniExcel、OpenXML 和其他第三方类型不得进入本包公开契约；稳定前必须冻结模板、校验、流生命周期、取消和错误报告语义。
