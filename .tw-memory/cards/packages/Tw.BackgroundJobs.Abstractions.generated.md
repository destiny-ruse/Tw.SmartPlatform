# Package: Tw.BackgroundJobs.Abstractions

标识：Tw.BackgroundJobs.Abstractions / backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs.Abstractions / platform-team
职责：提供后台任务定义、上下文、执行、控制与状态存储契约。

适用范围：
- 后台任务定义
- 后台任务上下文
- 控制命令
- 状态存储契约

不适用范围：
- Quartz 适配器
- MediatR 运行时管道

依赖边界：
- forbid: Quartz, SqlSugar*
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.BackgroundJobs.Abstractions
