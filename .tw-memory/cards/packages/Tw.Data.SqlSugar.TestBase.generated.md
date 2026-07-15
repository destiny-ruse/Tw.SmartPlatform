# Package: Tw.Data.SqlSugar.TestBase

标识：Tw.Data.SqlSugar.TestBase / backend/dotnet/BuildingBlocks/src/TestBase/Tw.Data.SqlSugar.TestBase / platform-team
职责：提供 SqlSugar 数据库测试夹具与重置辅助能力，生产项目不得引用该包。

适用范围：
- 数据库测试夹具
- Respawn 重置辅助能力

不适用范围：
- 生产数据库访问

依赖边界：
- forbid: 生产项目引用
- allow: Tw.TestBase, Tw.Data.SqlSugar, Testcontainers, Respawn

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Data.SqlSugar.TestBase
