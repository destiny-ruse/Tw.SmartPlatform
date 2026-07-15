# Package: Tw.Authorization

标识：Tw.Authorization / backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization / platform-team
职责：提供权限定义、授权上下文、稳定授权结果、权限 grant 存储与缓存边界，以及默认权限检查执行能力。

适用范围：
- 权限定义与授权上下文
- 稳定授权结果
- 权限检查接口与默认实现
- 权限 grant 存储读取边界
- 权限 grant 缓存边界与稳定缓存键

不适用范围：
- 权限 grant 持久化实现
- 分布式缓存实现
- JWT 发行和校验
- OpenIddict Identity Center

依赖边界：
- forbid: Microsoft.AspNetCore.*, OpenIddict*, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Authorization
