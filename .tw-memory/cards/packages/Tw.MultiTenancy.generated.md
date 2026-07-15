# Package: Tw.MultiTenancy

标识：Tw.MultiTenancy / backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy / platform-team
职责：提供与具体提供方无关的当前租户契约、租户身份值对象与认证后租户一致性解析。

适用范围：
- 当前租户契约
- 租户身份值对象
- 租户解析器
- 令牌租户与提示租户冲突检测

不适用范围：
- JWT 校验
- HTTP 租户访问器
- 特定提供方的租户存储
- 分片策略

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.MultiTenancy
