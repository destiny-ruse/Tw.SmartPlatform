# Package: Tw.Cli

标识：Tw.Cli / backend/dotnet/tools/src/Tw.Cli / dotnet-framework
职责：提供项目创建、能力接入、契约校验，以及基于仓库拓扑清单的依赖审计和诊断命令入口。

适用范围：
- `tw new` 项目创建入口
- `tw add capability` 能力接入入口
- `tw validate contracts` 契约校验入口
- `tw audit dependencies` 对项目、Directory.Build 与显式导入中的淘汰包、测试依赖和分层依赖执行保守审计
- `tw diagnose` 报告项目库存、解决方案一致性、项目引用、锁文件状态和 restore 未运行状态
- 通过具有十分钟超时和进程树终止边界的 `dotnet restore --locked-mode` 判定 NuGet 锁图是否陈旧

不适用范围：
- 业务运行时代码
- 微服务内的领域逻辑

依赖边界：
- forbid: runtime-only framework implementation packages outside command services
- allow: System.CommandLine, Spectre.Console

稳定性：experimental
兼容性：命令名称、usage 退出码 2、locked restore 超时退出码 124 和其他非零退出码作为自动化脚本契约保持稳定。
迁移指针：

source_refs:
- charter:package-charter:Tw.Cli
