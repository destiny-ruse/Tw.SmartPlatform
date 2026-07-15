# Tw.Excel.MiniExcel

`Tw.Excel.MiniExcel` 使用 MiniExcel 与 OpenXML 实现 `Tw.Excel` 的导出边界，当前公开 `MiniExcelExporter`。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须完成真实工作簿兼容性、流生命周期、取消、空模板、公式注入防护和大数据量行为验证。

## 边界

- provider 选择由宿主组合根负责
- MiniExcel 与 OpenXML 类型不得进入 `Tw.Excel` 公共契约
- 导入实现和业务字段映射不在当前包能力内
