# Package: Tw.Gateway

标识：Tw.Gateway / backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway / platform-team
职责：提供网关路由模型、可信请求头治理与网关限流策略契约。

适用范围：
- 网关路由模型
- 调用方传入身份请求头剥离
- 网关限流模型

不适用范围：
- YARP 适配器
- 业务授权
- 数据访问

依赖边界：
- forbid: Tw.Data*, Tw.Uow, Tw.Application, Tw.EventBus*, Tw.BackgroundJobs*, Tw.MultiTenancy, Tw.Sharding
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Gateway
