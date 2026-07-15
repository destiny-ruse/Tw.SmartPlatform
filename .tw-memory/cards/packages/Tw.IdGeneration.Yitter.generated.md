# Package: Tw.IdGeneration.Yitter

标识：Tw.IdGeneration.Yitter / backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration.Yitter / platform-team
职责：基于 Yitter.IdGenerator 的分布式 ID 生成适配器。

适用范围：
- Yitter ID 生成器适配
- WorkerId 初始化入口
- ID 生成服务注册入口

不适用范围：
- WorkerId 分配中心
- 数据库号段实现
- 雪花算法二次实现

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: Microsoft.Extensions.DependencyInjection.Abstractions, Tw.IdGeneration, Yitter.IdGenerator

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.IdGeneration.Yitter
