# Package: Tw.IdGeneration

标识：Tw.IdGeneration / backend/dotnet/BuildingBlocks/src/IdGeneration/Tw.IdGeneration / platform-team
职责：分布式 ID 生成的基础抽象契约。

适用范围：
- 长整型 ID 生成接口
- ID 生成调用契约

不适用范围：
- 具体 ID 算法实现
- WorkerId 分配服务
- 数据库序列实现

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.IdGeneration
