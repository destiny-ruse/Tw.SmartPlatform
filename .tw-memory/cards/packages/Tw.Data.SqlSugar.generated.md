# Package: Tw.Data.SqlSugar

标识：Tw.Data.SqlSugar / backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar / platform-team
职责：提供 SqlSugar 连接抽象与工作单元适配，并暴露用于 CAP Outbox 协调的事务边界。

适用范围：
- SqlSugar 客户端工厂抽象
- 连接配置解析
- SqlSugar 工作单元协调器
- 当前工作单元事务边界

不适用范围：
- CAP 传输
- ASP.NET Core 中间件
- Quartz 调度
- 网关路由

依赖边界：
- forbid: DotNetCore.CAP*, Microsoft.AspNetCore.*, Quartz, Yarp.*
- allow: SqlSugarCore, Tw.Data, Tw.Core

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Data.SqlSugar
