# Public API: Tw.EventBus.Cap

标识：Tw.EventBus.Cap / backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap

公开能力边界：
- Tw.EventBus.Cap

实现公开命名空间：
- Tw.EventBus.Cap
- Tw.EventBus.Cap.Consumers
- Tw.EventBus.Cap.Inbox
- Tw.EventBus.Cap.Outbox

公开类型：
- static class CapEventBusServiceCollectionExtensions - Tw.EventBus.Cap (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventBusServiceCollectionExtensions.cs:15)
- sealed class CapEventTransport - Tw.EventBus.Cap (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventTransport.cs:12)
- sealed class CapConsumerExecutionFilter - Tw.EventBus.Cap.Consumers (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Consumers/CapConsumerExecutionFilter.cs:8)
- sealed class SqlSugarInboxMessageStore - Tw.EventBus.Cap.Inbox (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Inbox/SqlSugarInboxMessageStore.cs:8)
- sealed class CapOutboxWriter - Tw.EventBus.Cap.Outbox (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/CapOutboxWriter.cs:9)
- interface IOutboxWriter - Tw.EventBus.Cap.Outbox (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/IOutboxWriter.cs:9)

DI 注册入口：
- Tw.EventBus.Cap.CapEventBusServiceCollectionExtensions.AddCapEventBus

包参考文档：
- docs/shared-packages/dotnet/Tw.EventBus.Cap/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.EventBus.Cap
