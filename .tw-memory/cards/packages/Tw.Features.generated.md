# Package: Tw.Features

标识：Tw.Features / backend/dotnet/BuildingBlocks/src/Application/Tw.Features / platform-team
职责：Feature definition、分作用域 feature value、feature cache key、feature store 边界和刷新请求。

适用范围：
- Feature 定义
- service、tenant、user 作用域 feature 值
- Feature 读取和刷新

不适用范围：
- Feature 管理 UI
- Feature 数据库实现
- 权限检查执行

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, OpenIddict*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Features
