# Package: Tw.Sharding

标识：Tw.Sharding / backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding / platform-team
职责：提供与具体提供方无关的分片描述契约与异步调用链分片上下文。

适用范围：
- 分片描述值对象
- 当前分片契约
- 分片上下文
- 作用域内分片切换

不适用范围：
- 数据库连接路由
- 分片路由策略
- 特定提供方的分片存储

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Sharding
