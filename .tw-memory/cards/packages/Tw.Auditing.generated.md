# Package: Tw.Auditing

标识：Tw.Auditing / backend/dotnet/BuildingBlocks/src/Auditing/Tw.Auditing / platform-team
职责：提供审计采集运行时、敏感审计明细脱敏与审计作用域。

适用范围：
- 审计采集器
- 审计作用域
- 敏感审计明细脱敏

不适用范围：
- 特定存储实现

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.Auditing.Contracts, Tw.Observability, Tw.Security

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Auditing
