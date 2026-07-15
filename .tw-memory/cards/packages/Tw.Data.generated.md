# Package: Tw.Data

标识：Tw.Data / backend/dotnet/BuildingBlocks/src/Data/Tw.Data / platform-team
职责：提供存储中立的数据访问、工作单元、事务边界、仓储与乐观并发检查契约。

适用范围：
- 工作单元及当前作用域协调契约
- 工作单元事务行为与 Outbox 事务边界
- 乐观并发检查契约
- 仓储抽象

不适用范围：
- 领域实体审计、并发戳与软删除标记
- SqlSugar 客户端创建
- CAP Outbox 存储
- 业务实体映射

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*, Microsoft.AspNetCore.*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Data
