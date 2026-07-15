# Tw.Cli

`Tw.Cli` 是仓库级 `tw` 命令行工具，用于项目创建、依赖审计、契约校验和仓库诊断。依赖审计与诊断都从 `backend/dotnet/BuildingBlocks/building-blocks-topology.json` 读取淘汰包映射，不在 CLI 中维护第二份清单。

## 安装

```powershell
dotnet pack backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -o artifacts/tools
dotnet tool install --tool-path artifacts/tool-home Tw.Cli --add-source artifacts/tools
```

## 命令

### 审计依赖

```powershell
dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -- audit dependencies --repository .
```

`audit dependencies` 扫描 `PackageReference` 和 `ProjectReference` 的 `Include`、`Update`，并按分号拆分静态 item-spec，检查以下边界：

- 拓扑清单中的淘汰包标识，匹配不区分大小写
- 生产项目对 TestBase 包的引用
- `Tw.AspNetCore` 对 Autofac、Castle 和基础设施提供程序的引用
- Application、Domain 项目对 SqlSugar、CAP、Quartz、YARP、Redis、Autofac 和 Castle 的引用

扫描器从仓库根到项目目录读取 `Directory.Build.props` 和 `Directory.Build.targets`，并递归读取静态显式 `Import`。为避免条件分支成为治理绕过，扫描器保守检查所有条件下声明的受治理引用；动态属性、item、metadata 或动态导入无法静态求值时返回 `TWGOV000`。导入循环通过已访问文件集合终止，导入不得越出仓库边界。`bin`、`obj`、模板目录与 `Tw.Templates/content` 不进入仓库项目枚举。

### 诊断仓库

```powershell
dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -- diagnose --repository .
```

`diagnose` 输出实际发现的 BuildingBlocks 运行时/测试项目数量、`.slnx` 一致性、未解析 `ProjectReference`、淘汰引用、缺失锁文件和锁文件内淘汰依赖。命令会执行以下权威检查，并同时传播子进程标准输出、标准错误和非零退出码：

```powershell
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
```

locked restore 默认超时为十分钟。超时时终止整个子进程树、排空标准输出和标准错误并返回稳定退出码 `124`。仓库或解决方案缺失导致 restore 未执行时，`locked restore exit code` 显示 `not run`，命令仍返回失败。

健康仓库的关键输出为 `source projects: 57`、`test projects: 50`、`solution parity: pass`，其余违规计数和 locked restore 退出码均为零。

### 其他入口

- `tw validate contracts --repository <path>`：执行契约校验入口
- `tw new`：显示项目模板入口
- `tw add capability`：显示能力接入入口

## 错误码与退出码

- `TWGOV000`：仓库路径、拓扑清单、项目 XML、导入 XML 或动态 MSBuild 身份无法静态治理
- `TWGOV002`：引用拓扑清单中的淘汰包
- `TWGOV003`：生产项目引用测试基础包
- `TWGOV004`：`Tw.AspNetCore` 引用基础设施提供程序
- `TWGOV005`：Application 或 Domain 项目引用基础设施提供程序

命令成功返回 `0`，依赖治理或仓库事实不满足时返回非零值，未知命令或 `--repository` 缺少路径值时返回 `2`。`diagnose` 的 locked restore 失败时返回该子进程的退出码，超时返回 `124`。
