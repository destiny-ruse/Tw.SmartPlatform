# BuildingBlocks 采纳前基线（2026-07）

## 用途与结论

本记录锁定 `backend/dotnet/BuildingBlocks/src` 在边界整改开始前的采纳前事实。基线提交 `9519b6bbf535e1b848bdddef19ea3c927bd58480` 是迁移前源码、公共 API、配置和协议的可恢复参照；回退和差异比较均以该提交为基准。

在下文区分的仓库证据与责任人确认共同成立时，所有 `Tw.*` 框架代码处于初始开发阶段，允许直接进行破坏性迁移。`Tw.Core` 的 charter 已从错误的 `stable` 标记修正为 `experimental`。边界清理必须在首个稳定基线建立前完成。

## 证据分类

### 直接仓库证据

- `NuGet.Config` 的有效源仅为 Huawei 和 nuget.org。
- `BuildingBlocks/src` 中 73 个 `.csproj` 的包名均未在上述两个源中找到精确匹配的预发行包。
- 排除 `BuildingBlocks` 与 `tools` 后，仓库内应用 `.csproj` 不包含 `PackageReference Include="Tw.`。
- `git tag --list` 没有输出。
- `artifacts` 下没有 `Tw.*.nupkg`。
- 模板中的内部包回退引用位于 `backend/dotnet/tools`，是模板工具输入，不是仓库应用消费者：`tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/Company.Gateway.Host.csproj` 在可找到 BuildingBlocks 项目时使用 `ProjectReference`，仅在 `UseRepositoryProjectReferences != true` 时启用 `PackageReference`；`tools/tests/Tw.Templates.Tests/TemplateSmokeTests.cs` 对该回退条件进行检查。

这些证据表明本仓库没有具体应用消费者，也没有公司已发布制品的仓库内痕迹。

### 用户、平台与发布责任人确认

以下事实来自用户、平台与发布责任人的明确确认，而非源列表命令本身：

- 所有 `Tw.*` 框架代码仍处于初始开发阶段。
- 此次边界整改允许破坏性变更。
- 不存在未列入 `backend/dotnet/NuGet.Config` 的内部源、CI 制品、稳定外部消费者或已发布稳定包。

`dotnet nuget list source` 只能列出配置中的源，不能证明不存在其他内部源；关于不存在单独稳定 `Tw.*` 发布源的结论依赖上述责任人确认。

## 可复核命令与结果

以下命令在基线提交上执行。包搜索在单次循环中完成，任一搜索失败会使命令以相同的退出码终止。

```powershell
dotnet nuget list source --configfile backend/dotnet/NuGet.Config

$packageIds = Get-ChildItem backend/dotnet/BuildingBlocks/src -Recurse -Filter *.csproj |
  ForEach-Object BaseName |
  Sort-Object -Unique
foreach ($packageId in $packageIds) {
  dotnet package search $packageId --exact-match --prerelease --format json --configfile backend/dotnet/NuGet.Config
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

git tag --list
git rev-parse HEAD

Get-ChildItem backend/dotnet -Recurse -Filter *.csproj |
  Where-Object { $_.FullName -notmatch '[\\/]BuildingBlocks[\\/]' -and $_.FullName -notmatch '[\\/]tools[\\/]' } |
  Select-String -Pattern '<PackageReference Include="Tw\.'

Get-ChildItem artifacts -Recurse -Filter 'Tw.*.nupkg' -ErrorAction SilentlyContinue
```

### 配置源输出

```text
注册的源:
  1.  Huawei [已启用]
      https://repo.huaweicloud.com/repository/nuget/v3/index.json
  2.  nuget.org [已启用]
      https://api.nuget.org/v3/index.json
```

### 精确预发行包搜索汇总

查询识别出 73 个唯一 `.csproj` 基名。每个 `dotnet package search --exact-match --prerelease --format json` 成功返回版本 2、空 `problems`，并分别对 `nuget.org` 与 Huawei 返回 `packages: []`。因此，配置源中没有任何清单包的精确匹配预发行包。

```text
Tw.Application
Tw.Application.Contracts
Tw.AspNetCore
Tw.AspNetCore.Abstractions
Tw.AspNetCore.Grpc
Tw.AspNetCore.Localization
Tw.AspNetCore.Mvc
Tw.AspNetCore.Mvc.NewtonsoftJson
Tw.AspNetCore.Swashbuckle
Tw.AspNetCore.TestBase
Tw.Auditing
Tw.Auditing.Contracts
Tw.Authorization
Tw.Authorization.Abstractions
Tw.BackgroundJobs
Tw.BackgroundJobs.Abstractions
Tw.BackgroundJobs.Quartz
Tw.Caching
Tw.Caching.FusionCache
Tw.Castle.Core
Tw.Configuration
Tw.Configuration.Json
Tw.Configuration.Nacos
Tw.Core
Tw.Data
Tw.Data.SqlSugar
Tw.Data.SqlSugar.TestBase
Tw.DependencyInjection
Tw.DependencyInjection.Abstractions
Tw.DependencyInjection.Autofac
Tw.DistributedLocking
Tw.DistributedLocking.Abstractions
Tw.DistributedLocking.Redis
Tw.Domain
Tw.Domain.Shared
Tw.EventBus
Tw.EventBus.Abstractions
Tw.EventBus.Cap
Tw.EventBus.Cap.TestBase
Tw.Excel
Tw.Excel.MiniExcel
Tw.ExceptionHandling
Tw.Features
Tw.Gateway
Tw.Gateway.Yarp
Tw.Grpc
Tw.Http
Tw.Http.Abstractions
Tw.Http.Client
Tw.Idempotency
Tw.Identity.OpenIddict
Tw.IdGeneration
Tw.IdGeneration.Yitter
Tw.Json.Abstractions
Tw.Json.Newtonsoft
Tw.Localization
Tw.MultiTenancy
Tw.MultiTenancy.Abstractions
Tw.Observability
Tw.Observability.OpenTelemetry
Tw.Observability.Serilog
Tw.Resilience
Tw.Security
Tw.Settings
Tw.Sharding
Tw.Sharding.Abstractions
Tw.TestBase
Tw.TextTemplating
Tw.TextTemplating.Scriban
Tw.Threading
Tw.Timing
Tw.Uow
Tw.Validation.Abstractions
```

### 标签、消费者与制品输出

| 查询 | 输出 | 结论 |
| --- | --- | --- |
| `git tag --list` | 无输出 | 没有仓库 Git tag |
| `git rev-parse HEAD` | `9519b6bbf535e1b848bdddef19ea3c927bd58480` | 可恢复的迁移前基线 |
| 应用 `Tw.*` `PackageReference` 搜索 | 无输出 | `BuildingBlocks` 与 `tools` 之外没有应用消费者 |
| `Get-ChildItem artifacts -Recurse -Filter 'Tw.*.nupkg'` | 无输出 | 没有仓库内 `Tw.*.nupkg` 制品 |

## 采纳前判定与稳定边界

共享包 charter 规范要求采纳前阶段同时满足“未被具体微服务或应用项目引用”与“未发布为 NuGet 包或其他外部制品”。本记录中的直接仓库证据满足前一条件并提供后一个条件的仓库内证据；后一个条件在仓库外的范围由责任人确认补足。

因此，本次整改可以删除或重命名公开类型与方法、迁移命名空间并调整服务注册入口，而不保留废弃转发壳或兼容别名。所有破坏性边界清理完成后，才可以建立首个 `stable` 基线；在此之前，`Tw.Core` 保持 `experimental`。
