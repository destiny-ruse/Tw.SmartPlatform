# Package: Tw.Gateway.Yarp

标识：Tw.Gateway.Yarp / backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway.Yarp / platform-team
职责：提供网关运行时的 YARP 路由校验与请求头转换边界。

适用范围：
- 路由校验
- 请求头转换工厂

不适用范围：
- YARP 服务注册与宿主装配
- 服务发现绑定
- 数据访问
- 工作单元
- 应用管道
- 事件总线与 CAP 消息
- 后台任务
- 多租户运行时
- 分片运行时

依赖边界：
- forbid: 数据访问包, 工作单元包, 应用管道包, 事件总线包, 后台任务包, 多租户运行时包, 分片运行时包
- allow: Tw.Gateway, Yarp.ReverseProxy, Microsoft.Extensions.ServiceDiscovery.Yarp

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Gateway.Yarp
