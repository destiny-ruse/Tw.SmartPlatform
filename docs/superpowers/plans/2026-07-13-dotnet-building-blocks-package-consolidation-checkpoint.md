# BuildingBlocks 包收敛执行检查点

## 交接目的

本检查点供下一次会话恢复执行
`docs/superpowers/plans/2026-07-11-dotnet-building-blocks-package-consolidation.md`。
继续前应先阅读该计划、本文档和仓库规则；本文档只记录已发生事实与尚未完成的门禁，不替代实施计划。

## 当前仓库状态

- 当前分支：`master`。
- 当前实现提交：`275c2ac9d5bf1e9cb007f5ed6d2a86ee0d597dca`，提交说明：`refactor: narrow core security and timing boundaries`。
- 写入本检查点前，`git status --short` 无输出，`git diff --check` 无输出。
- 用户明确同意不使用 Git worktree，在当前工作区执行。
- 用户确认全部 `Tw.*` 框架代码仍处于初始开发阶段，允许破坏性迁移；无稳定外部消费者、已发布稳定制品、隐藏内部源或 CI 制品需要兼容。
- 用户附加要求：删除项目时必须删除整个项目目录，不能留下源、测试、文档或仅含构建产物的空目录。

## 已完成任务

| 任务 | 提交 | 已完成内容与门禁 |
| --- | --- | --- |
| Task 0 | `ceb4ec8` | 记录预发布采纳基线，将 `Tw.Core` 标为 `experimental`，并记录无稳定消费者/制品的证据 |
| Task 1 | `7f6169c` | 建立唯一 topology manifest、solution parity 与退休目录门禁 |
| Task 2 | `1dc6379` | 用 `TWGOV001` 语义品牌标识符 Analyzer 取代旧前缀治理 |
| Task 3 | `8db37da` | 删除 Autofac、Castle 与通用动态代理路径，改用 Microsoft DI |
| Task 4 | `9e33180` | 删除 `Tw.Threading` 和 MVC 环境式取消，异步释放帮助器移至 `Tw.Async`，本地化 API 强制显式 `CancellationToken` |
| Task 5 | `275c2ac` | 加密迁入 `Tw.Security.Cryptography`，删除 `Tw.Timing`，迁移 `LocalizationConfigurationException` |

### Task 4 已验证结果

- 规范复核与质量复核均通过。
- `dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate` 成功。
- `Tw.Core.Tests`：7/7；`Tw.Localization.Tests`：34/34；`Tw.AspNetCore.Localization.Tests`：27/27；`Tw.AspNetCore.Mvc.Tests`：6/6；`Tw.Sharding.Tests`：2/2。
- `dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore`：0 warning、0 error。
- 架构筛选 `SolutionTopologyTests|PackageConsolidationTests`：14/14。
- 已确认 `Tw.Threading` 源与测试项目目录不存在，活动源码、测试、锁文件和共享包文档无 `Tw.Threading` 或 `ICancellationTokenProvider` 残留。

### Task 5 已验证结果

- 实现代理的红灯证据：Security 目标命名空间不存在（CS0234）、Localization 新异常不存在（CS0246）、`SecureRandomGenerator` 的 Security 目标类型不存在（CS0103）。
- `dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate` 成功。
- `Tw.Core.Tests`：7/7；`Tw.Security.Tests`：15/15；`Tw.Localization.Tests`：34/34；架构筛选：14/14。
- Task 5 规范复核已通过，确认：26 个原 Cryptography 文件保持算法/格式/失败语义，仅迁移命名空间；`Tw.Security -> Tw.Core` 为单向引用；`Tw.Timing` 源和测试完整目录不存在；新异常位于 `Tw.Localization` 且继承 `TwException`。
- 未列入原 26 文件但直接使用 `RandomNumberGenerator`、且无消费者的 `SecureRandomGenerator` 已迁入 `Tw.Security.Cryptography`；其范围、字节长度、无效参数与强密码基本契约已加入测试。`Tw.Core/Utilities` 仍仅保留泛用的 `DisposeAction` 与 `NullDisposable`。

## 当前待办

Task 5 尚未完成质量复核和主线程独立验证。下一次会话必须先完成以下门禁，确认通过后再把 Task 5 标为完成并开始 Task 6：

1. 派发只读质量评审，范围为 `9e33180..275c2ac`。重点检查已迁移加密 API 的异常、格式兼容和测试质量，`SecureRandomGenerator` 的行为测试，锁文件变更，以及文档与 charter 一致性。
2. 主线程重新执行计划 Task 5 Step 6 的命令：

   ```powershell
   dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
   dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Tw.Core.Tests.csproj --no-restore
   dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Tw.Security.Tests.csproj --no-restore
   dotnet test backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj --no-restore
   dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
   ```

3. 构建后再次确认以下目录均不存在：

   ```text
   backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/
   backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Timing.Tests/
   ```

4. 扫描活动源码、测试、`*.csproj`、锁文件、solution 与非迁移共享包文档，确认无 `Tw.Timing`、`TwConfigurationException`、`Tw.Core.Security.Cryptography` 或 Core 内 `System.Security.Cryptography` 残留。topology manifest、迁移基线和后续生成的 `.tw-memory` 历史数据可保留退休映射。
5. 运行 `git diff --check` 和 `git status --short`；仅在工作区清洁时继续 Task 6。

## 后续任务顺序

Task 6 至 Task 17 均未开始。严格按计划顺序执行：每个任务使用一个实现代理，随后依次进行规范复核、质量复核和主线程独立验证；每项删除均检查物理项目目录未重新出现。

## 环境注意事项

- Visual Studio 与 VS Code C# build host 处于运行状态。它们可能在解决方案 restore/build 后重新创建已退休项目下被忽略的 `bin` 或 `obj` 目录。
- 已在 Task 3、Task 4 和 Task 5 观察到这一现象。不要终止用户的 IDE 进程。若架构门禁发现已删除项目目录复现，先检查其内容和进程归属；确认仅为退休项目的忽略构建产物后，删除已核验的整个退休目录，再重跑架构门禁。
- `dotnet build` 第一次运行可能超过 60 秒。曾发生超时包装结束而其 `dotnet build` 子进程继续运行的情况。先检查该子进程是否自然结束，再以足够长的受控时限重跑，获取可记录的 exit code；不要与 IDE 或另一条 build/test 命令并发执行。
- `.tw-memory` 当前含旧路径生成卡片属于预期的后续 Task 16 治理范围，不能在 Task 5 作为活动文档残留误删。

## 继续时的用户意图

用户要求持续执行整份包收敛计划，而不是在每个任务后询问是否继续。只有实际阻塞、需要新授权或计划存在无法安全推断的重大歧义时才向用户提问。
