# Package: Tw.BackgroundJobs.Quartz

标识：Tw.BackgroundJobs.Quartz / backend/dotnet/BuildingBlocks/src/BackgroundJobs/Tw.BackgroundJobs.Quartz / platform-team
职责：提供 Quartz 调度器适配、Cron 校验、调度器存储选项与任务控制服务。

适用范围：
- Cron 表达式校验
- Quartz 调度器适配
- 后台任务控制服务

不适用范围：
- 业务任务处理器

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Tw.BackgroundJobs, Tw.BackgroundJobs.Abstractions, Quartz

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.BackgroundJobs.Quartz
