# Package: Tw.EventBus.Cap.TestBase

标识：Tw.EventBus.Cap.TestBase / backend/dotnet/BuildingBlocks/src/TestBase/Tw.EventBus.Cap.TestBase / platform-team
职责：提供 CAP 与 RabbitMQ 测试夹具以及 Outbox/Inbox 断言，生产项目不得引用该包。

适用范围：
- CAP RabbitMQ 测试夹具
- Outbox/Inbox 断言

不适用范围：
- 生产 CAP 传输

依赖边界：
- forbid: 生产项目引用
- allow: Tw.TestBase, Tw.EventBus.Cap, Testcontainers

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.EventBus.Cap.TestBase
