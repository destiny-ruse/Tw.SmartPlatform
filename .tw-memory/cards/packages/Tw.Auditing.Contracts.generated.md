# Package: Tw.Auditing.Contracts

标识：Tw.Auditing.Contracts / backend/dotnet/BuildingBlocks/src/Auditing/Tw.Auditing.Contracts / platform-team
职责：提供审计主体、动作、事件与存储的基础契约。

适用范围：
- 审计事件契约
- 审计存储抽象

不适用范围：
- 审计采集运行时
- 特定存储实现

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Auditing.Contracts
