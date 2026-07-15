# Package: Tw.EventBus.Cap

标识：Tw.EventBus.Cap / backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap / platform-team
职责：提供实验阶段的 CAP 传输绑定、RabbitMQ 选项、自定义 SqlSugar CAP 存储元数据、Inbox 去重、消费者上下文过滤器、 清理契约，以及绑定到当前 Tw.Data.Uow 工作单元事务的 Outbox 写入。当当前工作单元无法同时覆盖业务写入与 CAP Outbox 写入时拒绝发布。

适用范围：
- RabbitMQ 传输绑定
- 自定义 SqlSugar CAP 存储元数据
- Inbox 持久化契约
- CAP 消费者过滤器
- CAP 清理任务契约
- 当前 UoW Outbox 守卫

不适用范围：
- 业务事件契约
- SqlSugar 业务仓储实现
- ASP.NET Core 中间件

依赖边界：
- forbid: Microsoft.AspNetCore.*, Quartz, Yarp.*
- allow: DotNetCore.CAP, DotNetCore.CAP.RabbitMQ, Microsoft.Extensions.DependencyInjection.Abstractions, Tw.EventBus, Tw.Data, Tw.Data.SqlSugar

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.EventBus.Cap
