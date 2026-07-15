# Package: Tw.EventBus

标识：Tw.EventBus / backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus / platform-team
职责：提供集成事件元数据、发布器、处理器与 transport 契约，以及基于注入传输实现的发布编排。

适用范围：
- 集成事件元数据契约
- 事件发布器实现
- 事件处理器与传输契约

不适用范围：
- CAP 实现
- RabbitMQ 配置
- Outbox 存储

依赖边界：
- forbid: DotNetCore.CAP*, Microsoft.AspNetCore.*, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.EventBus
