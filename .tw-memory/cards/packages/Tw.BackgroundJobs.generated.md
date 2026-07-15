# Package: Tw.BackgroundJobs

标识：Tw.BackgroundJobs / backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs / platform-team
职责：提供通过 MediatR ISender 进入应用用例的后台任务运行时管道，并记录审计、追踪与指标事件。

适用范围：
- 后台任务命令
- 后台任务运行时管道
- 审计写入契约
- 追踪写入契约
- 指标写入契约

不适用范围：
- Quartz 调度器适配
- SQL 调度器存储

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.BackgroundJobs.Abstractions, MediatR

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.BackgroundJobs
