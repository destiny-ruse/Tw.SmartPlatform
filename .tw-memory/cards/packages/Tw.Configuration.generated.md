# Package: Tw.Configuration

标识：Tw.Configuration / backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration / platform-team
职责：提供配置来源治理、敏感配置键契约、配置变更事件，以及 JSON 配置清单与路径校验。

适用范围：
- 用户密钥环境策略
- 配置变更事件
- 敏感配置键模型
- JSON 配置文件清单
- 允许根目录的配置路径校验
- 通配符扫描拒绝规则

不适用范围：
- Nacos 配置源适配
- 服务发现
- 密钥存储

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*, Microsoft.AspNetCore.*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Configuration
