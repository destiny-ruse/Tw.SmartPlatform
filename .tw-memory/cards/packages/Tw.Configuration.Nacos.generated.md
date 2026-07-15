# Package: Tw.Configuration.Nacos

标识：Tw.Configuration.Nacos / backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Nacos / platform-team
职责：提供不承载密钥存储的 Nacos 配置源适配。

适用范围：
- 已校验配置键导入
- 配置变更事件

不适用范围：
- 密钥存储
- JSON 路径扫描
- 服务发现

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.Configuration, nacos-sdk-csharp, nacos-sdk-csharp.Extensions.Configuration

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Configuration.Nacos
