# Public API: Tw.EventBus

标识：Tw.EventBus / backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus

公开能力边界：
- Tw.EventBus

实现公开命名空间：
- Tw.EventBus

公开类型：
- sealed class EventPublisher - Tw.EventBus (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/EventPublisher.cs:7)
- interface IEventHandler - Tw.EventBus (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventHandler.cs:7)
- interface IEventPublisher - Tw.EventBus (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventPublisher.cs:6)
- interface IEventTransport - Tw.EventBus (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventTransport.cs:6)
- interface IIntegrationEvent - Tw.EventBus (backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IIntegrationEvent.cs:6)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.EventBus/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.EventBus
