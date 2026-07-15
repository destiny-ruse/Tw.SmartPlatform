# Package: Tw.Settings

标识：Tw.Settings / backend/dotnet/BuildingBlocks/src/Application/Tw.Settings / platform-team
职责：Setting definition、分作用域 setting value、setting cache key、setting store 边界和刷新请求。

适用范围：
- Setting 定义
- service、tenant、user 作用域 setting 值
- Setting 读取和刷新

不适用范围：
- Setting 管理 UI
- Setting 数据库实现
- 配置中心实现

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, OpenIddict*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Settings
