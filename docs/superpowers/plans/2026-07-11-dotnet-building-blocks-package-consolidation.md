# .NET BuildingBlocks Package Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `backend/dotnet/BuildingBlocks` 从 73 个生产项目收敛为设计批准的 57 个项目，同时把测试项目收敛为 50 个，并同步完成 solution folder、依赖、命名、工具、模板、charter、文档、锁文件和发布门禁重构。

**Architecture:** 以 capability 为物理目录、NuGet 和 solution folder 的共同边界；默认使用 Microsoft DI，删除 Autofac/Castle 通用动态代理主路径；入口横切逻辑使用入口原生管道，跨入口业务横切逻辑进入 `Tw.Application`；第三方实现只留在批准的 provider 边界。每次生产包迁移必须与对应测试、引用、charter、文档和 lock 同步完成，最终由精确 inventory、solution parity、retired reference、品牌命名和 pack/consume 门禁锁定。

**Tech Stack:** .NET 10、C#、MSBuild、Microsoft.Extensions.DependencyInjection、xUnit v3、AwesomeAssertions、Roslyn Analyzer、System.CommandLine、Python 3、pytest、YAML、NuGet lock files、`.slnx` XML。

---

## Scope and fixed decisions

- 文档类型：How-to / implementation plan；目标读者是执行迁移的 .NET 平台维护者和 agentic worker。
- 读者目标：按任务顺序完成破坏性结构迁移，并在每个提交边界得到可审查、可验证、可继续执行的绿色状态。
- 设计依据：`docs/superpowers/specs/2026-07-11-dotnet-building-blocks-package-consolidation-design.md`。
- BuildingBlocks 目标：57 个源码项目，其中 53 个运行时项目、4 个 TestBase 包；测试目标：50 个项目。
- 工具目标：`Tw.Analyzers`、`Tw.Cli`、`Tw.Templates` 保留在 `backend/dotnet/tools/src`，纳入 charter、文档和生成索引治理，但不计入 BuildingBlocks 的 57 个项目。
- 单一机器清单：`backend/dotnet/BuildingBlocks/building-blocks-topology.json` 是架构测试、CLI、模板、Python 治理和 pack/consume 脚本共同读取的唯一 inventory/retirement 数据源；附录 A/B 只是供评审阅读的镜像。
- 允许破坏性迁移，不建立旧 PackageId、旧命名空间或旧 API 的兼容转发包；实施前仍需确认没有已发布 stable 制品或已知稳定消费者。
- `TwException` 保留。PackageId、程序集名、`Tw.*` 根命名空间、`Tw:` 配置根和 `TWGOV000`—`TWGOV006` 诊断号不属于品牌标识符清理范围。
- provider 真实补实不在本计划内。Quartz、FusionCache、Nacos、SqlSugar、Redis、CAP、YARP、OpenTelemetry、gRPC host 与专项 TestBase 在本计划中只完成边界、引用、命名、文档准确性和 `experimental` 门禁；进入 stable 前分别编写专项 spec/plan。
- 所有移动使用 `git mv` 保留历史；所有文本修改使用 `apply_patch`；不得用全局字符串替换绕过语义审查。

### Retired production PackageIds

```text
Tw.Authorization.Abstractions
Tw.Domain.Shared
Tw.Configuration.Json
Tw.Uow
Tw.DistributedLocking.Abstractions
Tw.EventBus.Abstractions
Tw.Castle.Core
Tw.Threading
Tw.Timing
Tw.DependencyInjection.Autofac
Tw.Validation.Abstractions
Tw.Http.Abstractions
Tw.Http.Client
Tw.MultiTenancy.Abstractions
Tw.Sharding.Abstractions
Tw.AspNetCore.Abstractions
```

`Tw.Interception` 从未建立，但同样加入 reserved/forbidden catalog，防止用新名字恢复已否决的通用 AOP 主路径。

### Required green state at every task boundary

- 受影响项目 restore、build、test 通过。
- `.slnx` 与当时磁盘上的 `.csproj` 集合相等，且 source/test solution folder 与物理 capability folder 一致。
- 不留下指向已删除项目的 `ProjectReference`、solution entry、test project、charter、文档目录或 lock file。
- 若完整 `tw_memory check` 因尚未完成的后续迁移仍为红色，任务记录必须列出精确剩余项；Python 工具自身测试不得为红色。

本计划采用分阶段门禁：Task 1 在任何删除前锁定 57 个保留项目、retired 子集、测试迁移集合、真实 ProjectReference 和 solution parity；每个迁移任务同步更新 charter/docs。Task 14 把 retired 子集收紧为零，Task 16 修复当前发现 0 个真实 .NET 包的 Python 门禁并使 charter/docs/generated memory 全量转绿。这个顺序避免长期保留 suppression，同时也不把一个已知失真的 Python 检查伪装成早期绿色证据。

## Task 0: Record the prerelease adoption baseline

**Files:**

- Create: `docs/shared-packages/dotnet/migrations/2026-07-building-blocks-adoption-baseline.md`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml`

- [ ] **Step 1: Capture repository and package-source evidence**

  Record the user-approved facts that all framework code is in initial development and destructive changes are authorized. Add the output/summaries of:

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

  The expected repository result is no application consumer and no company-published artifact in scope. Template fallback references are test/tool inputs, not production consumers. Record the exact pre-migration commit SHA as the recoverable source/API/configuration/protocol baseline. Because the checked-in NuGet config currently lists only Huawei and nuget.org, also record the platform/release owner confirmation that there is no separate internal feed containing a stable `Tw.*` release; source listing alone is not evidence of absence.

- [ ] **Step 2: Correct the only false stable marker**

  Change `Tw.Core` from `stable` to `experimental` and state that the breaking boundary cleanup must finish before the first stable baseline. If the evidence unexpectedly finds a stable external consumer or stable artifact, stop only the affected PackageId migration and create a deprecation/dual-package track; do not silently continue a destructive change.

- [ ] **Step 3: Commit**

  ```powershell
  git add docs/shared-packages/dotnet/migrations/2026-07-building-blocks-adoption-baseline.md backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml
  git commit -m "docs: record building blocks prerelease baseline"
  ```

## Task 1: Lock physical and solution capability topology

**Files:**

- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/RepositoryLayout.cs`
- Create: `backend/dotnet/BuildingBlocks/building-blocks-topology.json`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageTopologyTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/SolutionTopologyTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageConsolidationTests.cs`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`

- [ ] **Step 1: Add solution-path and project-mapping helpers**

  Add `RepositoryLayout.SolutionFile`, normalized repository-relative path helpers, source/test capability extraction, and a single explicit owner exception for `Tw.DependencyInjection.Tests.Fixtures -> Tw.DependencyInjection`. Remove the `IsAbstractionsTestProject` skip from the runtime-mirroring path; only `Tw.Architecture.Tests` may lack a same-name runtime owner.

- [ ] **Step 2: Write failing solution topology tests**

  `SolutionTopologyTests` must parse `.slnx` with `XDocument` and implement:

  ```csharp
  [Fact]
  public void Solution_BuildingBlocksProjects_AreListedExactlyOnce()

  [Fact]
  public void Solution_BuildingBlocksProjectFolders_MirrorPhysicalCapabilityFolders()
  ```

  The first test compares all physical `BuildingBlocks/src/**/*.csproj` and `BuildingBlocks/tests/**/*.csproj` against all matching `<Project Path>` values, verifies uniqueness, and rejects missing paths. The second derives `/BuildingBlocks/src/<Capability>/` or `/BuildingBlocks/tests/<Capability>/` from each physical path and compares it to the containing `<Folder Name>`.

- [ ] **Step 3: Add phased migration-inventory tests before any deletion**

  Create `building-blocks-topology.json` with schema version, the exact runtime/test capability-relative paths from Appendices A/B, approved root namespace per runtime project (defaulting to the project stem, with the sole explicit `Tw.Core -> Tw` exception), the three tool project paths, the five approved independent contract packages, retired PackageId/replacement/runtime/test mappings, and retired namespaces. `RepositoryLayout` loads and validates this manifest; it does not duplicate the lists in C#. Add tests proving all 57 retained runtime projects already exist, every current runtime project is either an approved target or one of the 16 retired projects, every current test is either an approved target or one of the eight migrating test identities, and every current `ProjectReference` resolves to an existing `.csproj`. Treat `Tw.Http.Client.Tests` as the temporary predecessor of `Tw.Http.Tests`. These phased tests stay green while retired projects are removed but block unknown packages, accidental target deletion, and dangling references.

- [ ] **Step 4: Run the focused tests and confirm the expected failure**

  Run:

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests|FullyQualifiedName~BuildingBlocks_TestProjects_MirrorRuntimeCapabilityFolders"
  ```

  Expected: phased inventory/reference tests pass; project-set parity passes; folder-alignment fails because all 57 tests and 33 source projects are directly under the generic source/test folders.

- [ ] **Step 5: Reorganize `.slnx` without changing physical project paths**

  Move every BuildingBlocks `<Project Path>` under the exact capability folder derived from its path. `/BuildingBlocks/src/` and `/BuildingBlocks/tests/` may remain as empty parents, but must contain no direct `<Project>` children.

- [ ] **Step 6: Run topology and full architecture tests**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests|FullyQualifiedName~PackageTopologyTests"
  ```

  Expected: PASS; current 73 source and 57 test projects are each listed once and under matching solution folders.

- [ ] **Step 7: Commit**

  ```powershell
  git add backend/dotnet/Tw.SmartPlatform.slnx backend/dotnet/BuildingBlocks/building-blocks-topology.json backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests
  git commit -m "test: enforce building blocks solution topology"
  ```

## Task 2: Replace the prefix analyzer with semantic brand-segment governance

**Files:**

- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/ForbiddenIdentifierPrefixAnalyzer.cs`
- Create: `backend/dotnet/tools/src/Tw.Analyzers/Rules/ForbiddenBrandIdentifierAnalyzer.cs`
- Delete: `backend/dotnet/tools/tests/Tw.Analyzers.Tests/ForbiddenIdentifierPrefixAnalyzerTests.cs`
- Create: `backend/dotnet/tools/tests/Tw.Analyzers.Tests/ForbiddenBrandIdentifierAnalyzerTests.cs`
- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/ForbiddenPackageNameAnalyzer.cs`
- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/ForbiddenProjectReferenceAnalyzer.cs`
- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/DirectThirdPartyUsageAnalyzer.cs`
- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/UserSecretsEnvironmentAnalyzer.cs`
- Delete: `backend/dotnet/tools/src/Tw.Analyzers/Rules/LongIdExternalContractAnalyzer.cs`
- Modify: `backend/dotnet/tools/src/Tw.Analyzers/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.Analyzers/README.md`

- [ ] **Step 1: Write analyzer tests for true and false positives**

  Cover declarations named `TwOrderService`, `twOrder`, `TW_ORDER`, `AddTwYarpGateway`, `OrderTwHandler`, `AbpModule`, and `FurionService`; cover only the `Tw.Core` assembly's `Tw.Exceptions.TwException : Exception` type exemption; prove a method/local, another assembly, or unrelated type named `TwException` is still reported; and prove `Twin`, `Twice`, `Between`, and `Write` are not reported. Include types, methods, properties, fields, events, parameters, and local variable declarations. Rename the old branded test method to `ReportsForbiddenBrandSegmentExceptApprovedException`.

- [ ] **Step 2: Run the analyzer tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj --filter FullyQualifiedName~ForbiddenBrandIdentifierAnalyzerTests
  ```

  Expected: FAIL because the current analyzer is case-sensitive, prefix-only, type-only, and incorrectly treats normal `Tw...` words as prefixes.

- [ ] **Step 3: Implement identifier tokenization and declaration-only analysis**

  Split identifiers at `_`, lower-to-upper transitions, and acronym boundaries. Compare normalized tokens to `Tw`, `Abp`, and `Furion` case-insensitively. Analyze declaration symbols/syntax only; do not inspect namespaces, PackageIds, strings, comments, diagnostics, or type references. Return without reporting only for the named type `Tw.Exceptions.TwException` when it derives from `System.Exception` and `ContainingAssembly.Name == "Tw.Core"`; set the analyzer test compilation assembly name explicitly.

- [ ] **Step 4: Remove misleading analyzer stubs**

  The five static `*Analyzer` classes are DiagnosticId placeholders rather than Roslyn analyzers. Delete them and narrow the analyzer charter/README to the actually implemented `TWGOV001`. State that retired-package and dependency-boundary checks are owned by `Tw.Cli` plus architecture tests until dedicated Roslyn diagnostics are implemented.

- [ ] **Step 5: Run analyzer tests**

  ```powershell
  dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj
  ```

  Expected: PASS. Do not wire the analyzer repo-wide yet; Task 17 enables it after the known inventory is clean.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/tools/src/Tw.Analyzers backend/dotnet/tools/tests/Tw.Analyzers.Tests docs/shared-packages/dotnet/Tw.Analyzers
  git commit -m "feat: enforce semantic brand identifier rules"
  ```

## Task 3: Remove Autofac, Castle, and the generic dynamic-proxy path

**Files:**

- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Castle.Core/`
- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Autofac/`
- Delete: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Castle.Core.Tests/`
- Delete: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Autofac.Tests/`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/DependencyInjection/HostStartupBuilderExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/DependencyInjection/HostStartupBuilderExtensionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DynamicProxy/MvcInvocationContext.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DynamicProxy/PageInvocationContext.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DynamicProxy/TwActionInterceptionFilter.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DynamicProxy/TwPageInterceptionFilter.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/DynamicProxy/`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DependencyInjection/MvcIntegrationServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/Tw.AspNetCore.Mvc.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/TwAssemblyPriorityAttribute.cs` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection.Abstractions/AssemblyRegistrationPriorityAttribute.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Registration/ServicePriorityResolver.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/Tw.DependencyInjection.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.DependencyInjection/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Registration/ServicePriorityResolverTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Registration/ContainerNeutralRegistrationTests.cs`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/packages.lock.json`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Delete: `docs/shared-packages/dotnet/Tw.Castle.Core/`
- Delete: `docs/shared-packages/dotnet/Tw.DependencyInjection.Autofac/`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/service-registration.md`
- Modify: `docs/shared-packages/dotnet/Tw.DependencyInjection/assembly-scanning.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/host-startup.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/README.md`
- Delete: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/mvc-interception.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Rewrite tests around the approved Microsoft DI behavior**

  Replace the Autofac/Castle host test with assertions that `UseWebIntegration()` registers and resolves `IHostStartupSampleService` through Microsoft DI, returns the same builder, and exposes `ServiceRegistrationReport`. Remove interceptor fixture types and proxy report assertions. Add an assembly-reference assertion that `Tw.AspNetCore`, `Tw.AspNetCore.Mvc`, and `Tw.DependencyInjection` do not reference Autofac or Castle.

- [ ] **Step 2: Rename the assembly priority attribute in tests first**

  Add coverage that configuration priority wins over `[assembly: AssemblyRegistrationPriority(...)]`, and that type-level `ServicePriorityAttribute` behavior is unchanged.

- [ ] **Step 3: Run focused tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj
  ```

  Expected: FAIL until the host and MVC registration paths stop requiring Autofac/Castle and the renamed attribute exists.

- [ ] **Step 4: Make Microsoft DI the only registration executor**

  Remove `UseAutofac()`, the Autofac registration executor, Castle interception planning, proxy registration, and MVC dynamic-proxy filters. `UseWebIntegration()` must call the existing Microsoft DI service-registration path and must not select a container. Do not create `Tw.Interception` or a replacement generic proxy abstraction.

- [ ] **Step 5: Delete retired projects and keep solution parity green**

  Delete the two source projects and two test projects, remove their `.slnx` entries, update every affected `ProjectReference`, and remove the two Autofac friend-assembly entries from `Tw.DependencyInjection.csproj`. Rename `TwAssemblyPriorityAttribute` semantically in source, tests, XML docs, and package docs.

- [ ] **Step 6: Remove central Autofac/Castle versions and update accurate docs**

  Delete the five central package versions for Autofac/Castle. Documentation must describe Microsoft DI as the default path and direct Web/MVC users to middleware, authorization policies, MVC filters, endpoint filters, gRPC interceptors, CAP filters, Quartz listeners, or the application pipeline as appropriate.

- [ ] **Step 7: Restore and run affected tests**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet restore backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/Company.Gateway.Host.csproj --force-evaluate -p:UseRepositoryProjectReferences=true
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests|FullyQualifiedName~ForbiddenReferenceTests"
  ```

  Expected: PASS. Architecture/reference tests find no production/test PackageReference, ProjectReference, using, or assembly reference to Autofac/Castle. Negative charter rules, retired catalogs, and tests asserting absence may still contain those names and are not violations.

- [ ] **Step 8: Commit**

  ```powershell
  git add backend/dotnet docs/shared-packages
  git commit -m "refactor: remove autofac and castle runtime path"
  ```

## Task 4: Delete ambient cancellation and merge only async disposal helpers into Core

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/Async/AsyncDisposeFunc.cs` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Async/AsyncDisposeFunc.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/Async/NullAsyncDisposable.cs` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Async/NullAsyncDisposable.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Threading/`
- Delete: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Threading.Tests/`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Async/AsyncDisposeFuncTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Async/NullAsyncDisposableTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/TextLocalizer.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/EntityTranslationService.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Tw.Localization.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/TextLocalizerTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/EntityTranslationServiceTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/LocalizationServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/Tw.AspNetCore.Localization.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/Context/CancellationTokenServiceCollectionExtensions.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/Context/HttpContextCancellationTokenProvider.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/DependencyInjection/MvcIntegrationServiceCollectionExtensions.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Context/CancellationTokenServiceCollectionExtensionsTests.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Context/HttpContextCancellationTokenProviderTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/Tw.AspNetCore.Mvc.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/Tw.Sharding.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Delete: `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md`
- Delete: `docs/shared-packages/dotnet/Tw.AspNetCore/context/http-context-cancellation-token-provider.md`
- Delete: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc/context/http-context-cancellation-token-provider.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore.Localization/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add Core helper tests and explicit-cancellation tests**

  Test that `AsyncDisposeFunc` invokes its delegate exactly once, propagates delegate exceptions, and supports async disposal; test that `NullAsyncDisposable` is safely reusable. Update localization tests to pass a canceled token explicitly and assert cancellation originates from that token rather than ambient state.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Tw.Core.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj
  ```

  Expected: FAIL until helpers move and localization no longer depends on `ICancellationTokenProvider`.

- [ ] **Step 3: Move helpers and remove ambient cancellation**

  Change helper namespaces to `Tw.Async`; remove `ICancellationTokenProvider` constructor dependencies and registrations from core/Web localization; pass method tokens directly. Delete AsyncLocal cancellation overrides and MVC HttpContext cancellation services/registration. Remove the now-unnecessary `Tw.AspNetCore.Localization -> Tw.AspNetCore.Mvc` reference and the unused `Tw.Threading` reference from `Tw.Sharding`. Controllers/endpoints use their explicit token or `HttpContext.RequestAborted` at the call boundary.

- [ ] **Step 4: Delete projects and update references/docs/solution**

  Delete `Tw.Threading` and `Tw.Threading.Tests`, remove their solution entries and all project references, and update the Core charter so async disposal helpers are in scope while ambient cancellation remains out of scope.

- [ ] **Step 5: Restore and verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Tw.Core.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Sharding/Tw.Sharding.Tests/Tw.Sharding.Tests.csproj --no-restore
  dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS. Non-Architecture production/test declarations and project references contain neither `ICancellationTokenProvider` nor `Tw.Threading`; the topology manifest, retired catalogs, and migration docs may retain `Tw.Threading` as governed historical data.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: replace ambient cancellation with explicit tokens"
  ```

## Task 5: Move cryptography to Security, remove Timing, and localize configuration errors

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Security/Cryptography/` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Cryptography/`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Tw.Core.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/Tw.Security.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Cryptography/HashCompatibilityTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Cryptography/SymmetricCryptographyTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Cryptography/PasswordHasherTests.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Timing/`
- Delete: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Timing.Tests/`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Exceptions/TwConfigurationException.cs` -> `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationConfigurationException.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationOptions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/LocalizationServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/Json/JsonTextResourceParser.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/LocalizationOptionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/JsonTextResourceParserTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Localization/Tw.Localization/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Modify: `docs/shared-packages/dotnet/Tw.Core/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Security/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Localization/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add cryptographic compatibility and localization exception tests**

  Add fixed known vectors for SHA-256/SHA-3 and HMAC, deterministic password verification fixtures for the current PBKDF2 format, and symmetric encrypt/decrypt plus invalid-key/error tests. Update localization tests to require `LocalizationConfigurationException` in namespace `Tw.Localization`.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Tw.Security.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj
  ```

  Expected: FAIL because cryptography still lives in Core and the localization-specific exception does not exist.

- [ ] **Step 3: Move security code without changing algorithms or serialized formats**

  Move all 26 cryptography files to `Tw.Security.Cryptography` and update consumers. Add the required `Tw.Security -> Tw.Core` project reference for shared `Check` guards; the dependency must not reverse. This task changes ownership and namespace only; algorithm defaults, ciphertext layout, password hash format, key parsing, and failure behavior remain fixed by the new tests.

- [ ] **Step 4: Delete custom timing abstraction**

  Delete `Tw.Timing` and its tests/solution entries. There are no current production consumers. New time-dependent behavior must inject BCL `TimeProvider`; do not add a replacement framework interface.

- [ ] **Step 5: Move and rename the configuration exception**

  Keep `TwException` as the base class, but move the specialized exception to Localization and rename it `LocalizationConfigurationException`. Update XML docs, throw sites, tests, Core/Localization charters, and package docs. Preserve the `Tw.Core` `experimental` status established by Task 0 until the new public baseline is approved.

- [ ] **Step 6: Restore and verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Tw.Core.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Security.Tests/Tw.Security.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; `Tw.Core` no longer contains cryptography or `TwConfigurationException`; `Tw.Timing` is absent.

- [ ] **Step 7: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: narrow core security and timing boundaries"
  ```

## Task 6: Merge UoW into Data and move provider-neutral entity contracts to Domain

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Auditing/IAuditedEntity.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Auditing/IAuditedEntity.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Concurrency/IHasConcurrencyStamp.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Concurrency/IHasConcurrencyStamp.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Concurrency/IHasVersionStamp.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Concurrency/IHasVersionStamp.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/SoftDelete/ISoftDelete.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/SoftDelete/ISoftDelete.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/IUnitOfWork.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IUnitOfWork.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/IOutboxTransactionBoundary.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IOutboxTransactionBoundary.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/IUnitOfWorkManager.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/IUnitOfWorkCoordinator.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/UnitOfWorkOptions.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/UnitOfWorkOptions.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/UnitOfWorkTransactionBehavior.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Uow/UnitOfWorkTransactionBehavior.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Uow/`
- Move: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Uow.Tests/UnitOfWorkOptionsTests.cs` -> `backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.Tests/Uow/UnitOfWorkOptionsTests.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Uow.Tests/`
- Create: `backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.Tests/Uow/UnitOfWorkContractTests.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Application/Tw.Domain.Tests/EntityContractTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/Tw.Data.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Tw.Domain.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/Uow/SqlSugarUnitOfWork.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/Uow/SqlSugarUnitOfWorkManager.cs` -> `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/Uow/SqlSugarUnitOfWorkCoordinator.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/Tw.Data.SqlSugar.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data.SqlSugar/package-charter.yaml`
- Move: `backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.SqlSugar.Tests/Uow/SqlSugarUnitOfWorkManagerTests.cs` -> `backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.SqlSugar.Tests/Uow/SqlSugarUnitOfWorkCoordinatorTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.SqlSugar.Tests/Tw.Data.SqlSugar.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventTransport.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/IOutboxWriter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/CapOutboxWriter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Tw.EventBus.Cap.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/CapEventTransportTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Data/Tw.Data/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Modify: `docs/shared-packages/dotnet/Tw.Domain/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Data.SqlSugar/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.EventBus.Cap/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Move tests to their target owners first**

  Put `UnitOfWorkOptionsTests` in `Tw.Data.Tests`; add `UnitOfWorkContractTests` for namespace/interface shape, current scope, begin cancellation, commit/rollback/dispose obligations, and Outbox transaction completion; add Domain tests for audit fields, concurrency stamps, version stamps, and soft-delete markers; rename SqlSugar and CAP test doubles to `*UnitOfWorkCoordinator`. Change namespaces expected by tests to `Tw.Data.Uow` and `Tw.Domain.*`.

- [ ] **Step 2: Run affected tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Domain.Tests/Tw.Domain.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.Tests/Tw.Data.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.SqlSugar.Tests/Tw.Data.SqlSugar.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj
  ```

  Expected: FAIL until UoW and entity contracts move and the coordinator rename is implemented.

- [ ] **Step 3: Move contracts to the correct capability packages**

  Move provider-neutral entity shape interfaces to `Tw.Domain`; keep `IRepository`, `IConcurrencyCheckContext`, and `ConcurrencyConflictException` in `Tw.Data`. Move UoW, transaction, and Outbox transaction-boundary types to `Tw.Data.Uow`.

- [ ] **Step 4: Apply semantic coordinator naming**

  Rename `IUnitOfWorkManager` to `IUnitOfWorkCoordinator`, `SqlSugarUnitOfWorkManager` to `SqlSugarUnitOfWorkCoordinator`, and the CAP test doubles to `NullUnitOfWorkCoordinator`/`ActiveUnitOfWorkCoordinator`. Do not keep aliases for the old `Manager` names.

- [ ] **Step 5: Delete Uow projects and update references, solution, charters, and docs**

  Remove `Tw.Uow`/`Tw.Uow.Tests`, update SqlSugar/CAP references to `Tw.Data`, and make Data/Domain responsibilities explicit. Remove UoW claims from Foundation documentation.

- [ ] **Step 6: Restore and verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Domain.Tests/Tw.Domain.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.Tests/Tw.Data.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Data/Tw.Data.SqlSugar.Tests/Tw.Data.SqlSugar.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj --no-restore
  dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS. Production/test declarations and references contain neither `Tw.Uow` nor `UnitOfWorkManager`; retired catalogs, migration docs, and negative charter rules may still name `Tw.Uow` intentionally.

- [ ] **Step 7: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: merge unit of work into data"
  ```

## Task 7: Consolidate Application contracts, Authorization, and Domain.Shared

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/AuthorizationContext.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/AuthorizationContext.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/AuthorizationResult.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/AuthorizationResult.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IGrantStore.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IGrantStore.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IPermissionChecker.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IPermissionChecker.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/IPermissionGrantCache.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/IPermissionGrantCache.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/PermissionDefinition.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionDefinition.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/PermissionGrantCacheKey.cs` -> `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionGrantCacheKey.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization.Abstractions/`
- Delete: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain.Shared/`
- Delete: `backend/dotnet/BuildingBlocks/tests/Application/Tw.Domain.Shared.Tests/`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/PermissionChecker.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/Tw.Authorization.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Application/Tw.Authorization.Tests/PermissionCheckerTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Application/Tw.Authorization.Tests/Tw.Authorization.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/Tw.Application.Contracts.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/Tw.Domain.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Domain/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Application.Contracts/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Application/Tw.Authorization/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Delete: `docs/shared-packages/dotnet/Tw.Authorization.Abstractions/`
- Delete: `docs/shared-packages/dotnet/Tw.Domain.Shared/`
- Modify: `docs/shared-packages/dotnet/Tw.Authorization/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Domain/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Expand Authorization contract tests in the retained test project**

  Add or move tests for `AuthorizationResult`, permission definitions, grant cache keys, null/empty grant behavior, and the default `PermissionChecker`. `Tw.Authorization.Tests` must reference only `Tw.Authorization`.

- [ ] **Step 2: Run retained tests and confirm failure after test reference change**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Authorization.Tests/Tw.Authorization.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Application.Contracts.Tests/Tw.Application.Contracts.Tests.csproj
  ```

  Expected: FAIL until the contracts move and `Tw.Application.Contracts` stops referencing `Tw.Domain.Shared`.

- [ ] **Step 3: Merge Authorization and delete global Domain.Shared**

  Preserve the company-owned authorization interfaces but move their namespaces to `Tw.Authorization`; delete the thin package and do not retain a misleading `.Abstractions` namespace. Delete the empty global Domain.Shared package/test project. Remove its reference from both `Tw.Domain` and `Tw.Application.Contracts`. Do not move business DTOs or shared enums into `Tw.Domain` or Application.Contracts.

- [ ] **Step 4: Update solution, charters, docs, and locks**

  Remove three retired solution entries, describe Application.Contracts as MediatR/FluentValidation/provider-neutral, and document that service-specific shared contracts live inside each bounded context.

- [ ] **Step 5: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Authorization.Tests/Tw.Authorization.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Application.Contracts.Tests/Tw.Application.Contracts.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Application/Tw.Domain.Tests/Tw.Domain.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; retired Application project/test paths are absent.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: consolidate application capability packages"
  ```

## Task 8: Merge JSON configuration governance into Configuration

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json/ConfigurationPathException.cs` -> `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/Json/ConfigurationPathException.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json/JsonConfigurationBuilderExtensions.cs` -> `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/Json/JsonConfigurationBuilderExtensions.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json/JsonConfigurationManifest.cs` -> `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/Json/JsonConfigurationManifest.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json/JsonConfigurationPathValidator.cs` -> `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/Json/JsonConfigurationPathValidator.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Json/`
- Move: `backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Json.Tests/JsonConfigurationPathValidatorTests.cs` -> `backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Tests/Json/JsonConfigurationPathValidatorTests.cs`
- Delete: `backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Json.Tests/`
- Modify: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/Tw.Configuration.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Tests/Tw.Configuration.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Nacos/Tw.Configuration.Nacos.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Configuration/Tw.Configuration.Nacos/package-charter.yaml`
- Modify: `backend/dotnet/Build/Packages.Framework.props`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `docs/shared-packages/dotnet/Tw.Configuration.Nacos/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Configuration/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Move the path-validator test into the retained test project**

  Preserve the existing outside-allowed-root assertion, then add traversal, missing-file, manifest, and normalized-path cases. Use test namespace `Tw.Configuration.Tests.Json`, import production namespace `Tw.Configuration.Json`, and change the test project's `ProjectReference` to `Tw.Configuration`.

- [ ] **Step 2: Run the retained test and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Tests/Tw.Configuration.Tests.csproj
  ```

  Expected: FAIL until JSON configuration types are compiled by the retained package.

- [ ] **Step 3: Merge source and narrow Nacos scope**

  Move JSON configuration files under the retained project, preserving a `Tw.Configuration.Json` subnamespace if useful. Remove `nacos-sdk-csharp.Extensions.ServiceDiscovery` from `Tw.Configuration.Nacos` and its central version if no other project uses it; this package owns configuration source integration only.

- [ ] **Step 4: Delete old projects and update solution/docs/locks**

  Remove `Tw.Configuration.Json` source/test entries and merge charter responsibilities. The Nacos README must explicitly say service discovery is out of scope and the package remains experimental until real provider integration tests exist.

- [ ] **Step 5: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Configuration/Tw.Configuration.Tests/Tw.Configuration.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; Configuration has 2 source projects and 1 test project, and no Nacos service-discovery package reference remains.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Build backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: merge json configuration capability"
  ```

## Task 9: Merge DistributedLocking and EventBus abstraction packages

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Abstractions/DistributedLockKey.cs` -> `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking/DistributedLockKey.cs`
- Move: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Abstractions/IDistributedLock.cs` -> `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking/IDistributedLock.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking/DistributedLockKeyBuilder.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Redis/RedisDistributedLock.cs`
- Move: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/IEventHandler.cs` -> `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventHandler.cs`
- Move: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/IEventPublisher.cs` -> `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventPublisher.cs`
- Move: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/IEventTransport.cs` -> `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IEventTransport.cs`
- Move: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/IIntegrationEvent.cs` -> `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/IIntegrationEvent.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/EventPublisher.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventBusServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/CapEventTransport.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/CapOutboxWriter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Outbox/IOutboxWriter.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking/Tw.DistributedLocking.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Redis/Tw.DistributedLocking.Redis.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking.Redis/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/DistributedLocking/Tw.DistributedLocking.Tests/Tw.DistributedLocking.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/Tw.EventBus.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/Tw.EventBus.Cap.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus.Cap/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Tests/Tw.EventBus.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Tests/EventPublisherTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/CapEventTransportTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/DistributedLocking/Tw.DistributedLocking/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/EventBus/Tw.EventBus/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `docs/shared-packages/dotnet/Tw.DistributedLocking.Redis/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.DistributedLocking/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.EventBus/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.EventBus.Cap/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Expand retained contract tests**

  Add lock-key equality/validation and lock cancellation/ownership contract tests in `Tw.DistributedLocking.Tests`; add integration-event metadata, publisher dispatch, cancellation, and transport-failure propagation tests in `Tw.EventBus.Tests`.

- [ ] **Step 2: Point tests/providers to retained packages and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/DistributedLocking/Tw.DistributedLocking.Tests/Tw.DistributedLocking.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Tests/Tw.EventBus.Tests.csproj
  ```

  Expected: FAIL until the contract types move.

- [ ] **Step 3: Merge sources and update provider references**

  Preserve provider-neutral interfaces in the retained packages and change their namespaces to `Tw.DistributedLocking` and `Tw.EventBus`. Redis and CAP reference those packages directly. Do not retain retired `.Abstractions` namespaces or expose StackExchange.Redis/CAP types from capability contracts.

- [ ] **Step 4: Delete old projects and update solution/charters/docs**

  Remove both abstraction projects and solution entries. Mark Redis/CAP provider docs experimental and state real lease/fencing or delivery/Inbox/Outbox verification is a separate stable gate.

- [ ] **Step 5: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/DistributedLocking/Tw.DistributedLocking.Tests/Tw.DistributedLocking.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Tests/Tw.EventBus.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; no project reference targets either retired abstraction package.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: consolidate locking and event contracts"
  ```

## Task 10: Consolidate outbound HTTP and make Resilience provider-neutral

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Abstractions/HeaderPropagationOptions.cs` -> `backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HeaderPropagation/HeaderPropagationOptions.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Client/HeaderPropagation/HeaderPropagationPolicy.cs` -> `backend/dotnet/BuildingBlocks/src/Http/Tw.Http/HeaderPropagation/HeaderPropagationPolicy.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Abstractions/`
- Delete: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Client/`
- Move: `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Client.Tests/` -> `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/`
- Rename: `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/Tw.Http.Client.Tests.csproj` -> `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/Tw.Http.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http/Tw.Http.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/Tw.Http.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/HeaderPropagationPolicyTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http/package-charter.yaml`
- Delete: `backend/dotnet/BuildingBlocks/src/Resilience/Tw.Resilience/HttpResilienceServiceCollectionExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Resilience/Tw.Resilience/Tw.Resilience.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Resilience/Tw.Resilience/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/tests/Resilience/Tw.Resilience.Tests/ResiliencePolicyBuilderTests.cs`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Delete: `docs/shared-packages/dotnet/Tw.Http.Client/`
- Create: `docs/shared-packages/dotnet/Tw.Http/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Resilience/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Rename the HTTP test project and strengthen behavior tests**

  Change namespace/project identity to `Tw.Http.Tests`; verify allow-listed header propagation, case-insensitive matching, absent headers, immutable input, and forbidden sensitive headers. Reference only `Tw.Http`.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/Tw.Http.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Resilience/Tw.Resilience.Tests/Tw.Resilience.Tests.csproj
  ```

  Expected: FAIL until the new project/type paths exist and Resilience no longer carries HTTP registration.

- [ ] **Step 3: Merge HTTP source and remove no-op resilience API**

  Use namespace `Tw.Http.HeaderPropagation`. Remove `Microsoft.Extensions.Http.Resilience` from the old Client path and delete `AddTwHttpResilience`; do not create `AddHttpResilience` until a real, tested outgoing HTTP registration exists. Remove Polly and `Microsoft.Extensions.Http.Resilience` from `Tw.Resilience`; retain only company-owned policy descriptors, operation classification, and validation.

- [ ] **Step 4: Delete retired projects and update solution/docs/charters**

  `Tw.Http` becomes the only HTTP source project and `Tw.Http.Tests` its only test project. Document retry idempotency restrictions and state that concrete HTTP handlers belong in `Tw.Http`, not the provider-neutral policy package.

- [ ] **Step 5: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Http/Tw.Http.Tests/Tw.Http.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Resilience/Tw.Resilience.Tests/Tw.Resilience.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; only `Tw.Http`/`Tw.Http.Tests` remain in the HTTP capability.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: consolidate outbound http capability"
  ```

## Task 11: Merge MultiTenancy and Sharding abstraction packages

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions/CurrentTenant.cs` -> `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/CurrentTenant.cs`
- Move: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions/ICurrentTenant.cs` -> `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/ICurrentTenant.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/TenantResolver.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions/IShardContext.cs` -> `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/IShardContext.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions/ShardDescriptor.cs` -> `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/ShardDescriptor.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/ShardContext.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/Tw.MultiTenancy.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/MultiTenancy/Tw.MultiTenancy.Tests/Tw.MultiTenancy.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/Tw.Sharding.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Sharding/Tw.Sharding.Tests/Tw.Sharding.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Sharding/Tw.Sharding.Tests/ShardContextTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/MultiTenancy/Tw.MultiTenancy/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Sharding/Tw.Sharding/package-charter.yaml`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Modify: `docs/shared-packages/dotnet/Tw.MultiTenancy/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Sharding/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Expand retained contract tests**

  In `Tw.MultiTenancy.Tests`, cover `CurrentTenant.Default`, token-only, hint-only, equal token/hint, and mismatched token/hint behavior already exposed by `TenantResolver`. In `Tw.Sharding.Tests`, cover `ShardDescriptor.None`, value equality, `Change(null)`, nested scope restore, and idempotent scope disposal. Do not invent tenant-ID validation or shard-router APIs in this structural task. Point each test project only at the retained package.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/MultiTenancy/Tw.MultiTenancy.Tests/Tw.MultiTenancy.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Sharding/Tw.Sharding.Tests/Tw.Sharding.Tests.csproj
  ```

  Expected: FAIL until contract types move.

- [ ] **Step 3: Merge sources and delete thin projects**

  Keep contract interfaces and provider-neutral implementations in `Tw.MultiTenancy`/`Tw.Sharding`, and collapse their namespaces to those retained roots. Remove two project references, retired project directories, and solution entries. Preserve no retired `.Abstractions` namespace, provider-specific tenant store, HTTP accessor, or database routing type in these core packages.

- [ ] **Step 4: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/MultiTenancy/Tw.MultiTenancy.Tests/Tw.MultiTenancy.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Sharding/Tw.Sharding.Tests/Tw.Sharding.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; each capability now has one source and one test project.

- [ ] **Step 5: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: consolidate tenancy and sharding contracts"
  ```

## Task 12: Merge AspNetCore abstractions and apply Web semantic names

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions/AuthenticationSchemeNames.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Authentication/AuthenticationSchemeNames.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions/ProtocolError.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Errors/ProtocolError.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions/RequestCorrelation.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Correlation/RequestCorrelation.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Tw.AspNetCore.csproj`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Health/HealthEndpointRouteBuilderExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Health/HealthEndpointRouteBuilderExtensionsTests.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/TwStringLocalizer.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizer.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/TwStringLocalizerOfT.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizerOfT.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/TwStringLocalizerFactory.cs` -> `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/StaticSnapshotStringLocalizerFactory.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/LocalizationServiceCollectionExtensions.cs`
- Move: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/TwStringLocalizerTests.cs` -> `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/StaticSnapshotStringLocalizerTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/LocalizationServiceCollectionExtensionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/package-charter.yaml`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/packages.lock.json`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Delete: `docs/shared-packages/dotnet/Tw.AspNetCore.Abstractions/`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.AspNetCore.Localization/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Put contract and naming expectations in retained tests**

  Add contract-shape tests for authentication schemes, protocol errors, and request correlation to `Tw.AspNetCore.Tests`. Add a route test that `MapHealthEndpoint()` maps exactly `/health` and returns the same endpoint builder. Rename localizer tests/classes to `StaticSnapshotStringLocalizerTests` and require the static-snapshot implementations/factory from DI.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj
  ```

  Expected: FAIL until contracts move and semantic Web names replace brand names.

- [ ] **Step 3: Merge AspNetCore contracts**

  Move the three provider-neutral ASP.NET Core contracts into the retained package. Delete the abstraction project/solution entry and update all Web package/test references. Protocol mapping remains inside each entry adapter; do not move gRPC or CAP error types into this package.

- [ ] **Step 4: Rename health and localization APIs**

  Rename `MapTwHealthEndpoints` to singular `MapHealthEndpoint`. Rename `TwStringLocalizer`, `TwStringLocalizer<T>`, and `TwStringLocalizerFactory` to the static-snapshot names in source, registrations, tests, XML docs, and usage docs. Remove the `TwLocalizationOptions` alias and use `LocalizationOptions` directly (or `CoreLocalizationOptions` only where an alias is required for ambiguity).

- [ ] **Step 5: Verify**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet restore backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/Company.Gateway.Host.csproj --force-evaluate -p:UseRepositoryProjectReferences=true
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Web/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --no-restore --filter "FullyQualifiedName~SolutionTopologyTests|FullyQualifiedName~PackageConsolidationTests"
  ```

  Expected: PASS; `Tw.AspNetCore.Abstractions` and the old Web identifiers are absent.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: consolidate aspnetcore contracts and names"
  ```

## Task 13: Merge validation errors and finish semantic naming cleanup

**Files:**

- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions/ValidationError.cs` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/Validation/ValidationError.cs`
- Move: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions/ValidationException.cs` -> `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/Validation/ValidationException.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.Validation.Abstractions/`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/Tw.ExceptionHandling.csproj`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/DefaultExceptionToErrorMapper.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.ExceptionHandling.Tests/Validation/ValidationExceptionTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.ExceptionHandling.Tests/DefaultExceptionToErrorMapperTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Foundation/Tw.ExceptionHandling/package-charter.yaml`
- Delete: `backend/dotnet/BuildingBlocks/src/Gateway/Tw.Gateway.Yarp/YarpGatewayBuilderExtensions.cs`
- Delete: `backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.OpenTelemetry/OpenTelemetryBuilderExtensions.cs`
- Modify: `backend/dotnet/BuildingBlocks/src/Observability/Tw.Observability.Serilog/SerilogBuilderExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/tests/Observability/Tw.Observability.Serilog.Tests/SerilogBuilderExtensionsTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Discovery/RuntimeAssemblySourceTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Discovery/AssemblyFilterTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.DependencyInjection.Tests/Discovery/AssemblyDiscovererTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Foundation/Tw.Core.Tests/Reflection/ReflectionNamespaceTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Localization/Tw.Localization.Tests/InterfaceShapeTests.cs`
- Modify: `backend/dotnet/Tw.SmartPlatform.slnx`
- Create: `docs/shared-packages/dotnet/Tw.ExceptionHandling/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Gateway.Yarp/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Observability.OpenTelemetry/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Observability.Serilog/README.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add validation and Serilog behavior tests**

  Test immutable validation-error capture, null input, stable error mapping, and preservation of field path/code/message under `Tw.ExceptionHandling.Validation`. Test that `EnrichWithSensitiveDataRedaction()` installs the redacting enricher, rejects null arguments, and redacts a representative sensitive property.

- [ ] **Step 2: Run tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.ExceptionHandling.Tests/Tw.ExceptionHandling.Tests.csproj
  dotnet test backend/dotnet/BuildingBlocks/tests/Observability/Tw.Observability.Serilog.Tests/Tw.Observability.Serilog.Tests.csproj
  ```

  Expected: FAIL until validation moves and the Serilog extension is renamed.

- [ ] **Step 3: Merge validation and remove its project**

  Use namespace `Tw.ExceptionHandling.Validation`; teach the default mapper to preserve structured validation errors; delete the thin source project and solution entry. No separate validation test project is created.

- [ ] **Step 4: Remove no-op provider entry points and rename the real one**

  Delete `AddTwYarpGateway`, `AddTwOpenTelemetry`, and any remaining `AddTwHttpResilience` because they currently return the input service collection without real wiring. Keep provider options/validators that have behavior and keep the packages experimental. Rename the real Serilog extension to `EnrichWithSensitiveDataRedaction`.

- [ ] **Step 5: Rename misleading test identifiers**

  Apply the audited names:

  - `IncludesLoadedTwAssemblies` -> `IncludesLoadedDefaultPrefixAssemblies`
  - `TwPrefix` test-name segments -> `DefaultAssemblyPrefix`
  - `Discover_FiltersToTwPrefix...` -> `Discover_FiltersToDefaultPrefix...`
  - `*TwReflectionNamespace` -> `*ReflectionNamespace`
  - `PublicInterfaces_LiveInTwLocalizationNamespace` -> `PublicInterfaces_LiveInLocalizationNamespace`

  Keep `AssemblyFilter.DefaultPrefix = "Tw."` because it is an assembly namespace convention, not a branded code identifier name.

- [ ] **Step 6: Verify affected packages and the analyzer**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  dotnet test backend/dotnet/BuildingBlocks/tests/Foundation/Tw.ExceptionHandling.Tests/Tw.ExceptionHandling.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Observability/Tw.Observability.Serilog.Tests/Tw.Observability.Serilog.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Gateway/Tw.Gateway.Yarp.Tests/Tw.Gateway.Yarp.Tests.csproj --no-restore
  dotnet test backend/dotnet/BuildingBlocks/tests/Observability/Tw.Observability.OpenTelemetry.Tests/Tw.Observability.OpenTelemetry.Tests.csproj --no-restore
  dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj --no-restore
  ```

  Expected: PASS. The only intentional production identifier beginning with the brand token is `TwException`; analyzer negative test source may still contain `TwOrderService`.

- [ ] **Step 7: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks backend/dotnet/Tw.SmartPlatform.slnx docs/shared-packages
  git commit -m "refactor: merge validation and clean semantic names"
  ```

## Task 14: Lock the exact inventories and retired-reference boundaries

**Files:**

- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/RepositoryLayout.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageTopologyTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/ForbiddenReferenceTests.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/PackageConsolidationTests.cs`
- Modify: `backend/dotnet/tools/src/Tw.Cli/Governance/ForbiddenPackageCatalog.cs`
- Modify: `backend/dotnet/tools/src/Tw.Cli/Governance/ProjectDependencyScanner.cs`
- Modify: `backend/dotnet/tools/src/Tw.Cli/Program.cs`
- Modify: `backend/dotnet/tools/tests/Tw.Cli.Tests/AuditDependenciesCommandTests.cs`
- Modify: `backend/dotnet/tools/src/Tw.Cli/package-charter.yaml`
- Modify: `docs/shared-packages/dotnet/Tw.Cli/README.md`

- [ ] **Step 1: Add exact approved inventory tests**

  Implement the following tests from `backend/dotnet/BuildingBlocks/building-blocks-topology.json`; Appendices A/B must mirror the same capability-relative paths for human review:

  ```csharp
  [Fact] public void RuntimeProjectSet_MatchesApprovedConsolidatedTopology()
  [Fact] public void TestProjectSet_MatchesApprovedConsolidatedTopology()
  [Fact] public void RetiredPackageDirectories_DoNotExist()
  [Fact] public void ProjectReferences_DoNotTargetRetiredPackages()
  [Fact] public void RetiredNamespaces_DoNotRemainInSource()
  [Fact] public void ProjectReferences_ResolveToExistingProjects()
  [Fact] public void TestProjects_TargetExistingRuntimePackages()
  [Fact] public void RuntimeProjectIdentity_MatchesDirectoryAndPackageMetadata()
  [Fact] public void OwnedTypeNames_DoNotUseAmbiguousRoleSuffixes()
  ```

  Compare normalized `Capability/Project/Project.csproj` paths, not only names or counts. For every runtime project, require directory name and `.csproj` stem to match; explicit `PackageId` and `AssemblyName` must equal that stem, while omitted values use the same MSBuild default. `RootNamespace` must equal the project stem except the approved `Tw.Core -> Tw` root, which preserves its existing `Tw.Check`, `Tw.Collections`, `Tw.Exceptions`, and related public namespaces. Require a `Tw.*` PackageId. Scan namespace declarations and reject retired boundaries (`Tw.Authorization.Abstractions`, `Tw.Domain.Shared`, `Tw.Uow`, `Tw.DistributedLocking.Abstractions`, `Tw.EventBus.Abstractions`, `Tw.Castle.Core`, `Tw.Threading`, `Tw.Timing`, `Tw.DependencyInjection.Autofac`, `Tw.Validation.Abstractions`, `Tw.Http.Abstractions`, `Tw.Http.Client`, `Tw.MultiTenancy.Abstractions`, `Tw.Sharding.Abstractions`, `Tw.AspNetCore.Abstractions`, `Tw.Interception`); allow `Tw.Configuration.Json` as a functional subnamespace. Scan owned type declarations and reject `Manager`, `Helper`, and `Util` suffixes; third-party references such as `YitIdHelper` are not owned declarations and are not violations. Keep the five approved independently packaged contract projects explicit: `Tw.Application.Contracts`, `Tw.Auditing.Contracts`, `Tw.BackgroundJobs.Abstractions`, `Tw.DependencyInjection.Abstractions`, and `Tw.Json.Abstractions`.

- [ ] **Step 2: Prove the tests catch a retired reference**

  Temporarily add a `Tw.Http.Client` fixture value inside the test arrangement, run the focused test and observe failure, then remove the fixture mutation before continuing. This proves the gate is active without leaving a red commit.

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter FullyQualifiedName~PackageConsolidationTests
  ```

- [ ] **Step 3: Add retired mappings and layer rules to `Tw.Cli`**

  Change `ForbiddenPackageCatalog` to load the 16 retired PackageIds/replacements plus reserved `Tw.Interception` from `backend/dotnet/BuildingBlocks/building-blocks-topology.json`; do not duplicate the list in C#. Match case-insensitively and scan both `PackageReference` and `ProjectReference`. Add rules that `Tw.AspNetCore` cannot reference Autofac, Castle, or infrastructure providers, and Application/Domain projects cannot reference SqlSugar, CAP, Quartz, YARP, Redis implementations, Autofac, or Castle.

- [ ] **Step 4: Replace hard-coded diagnose output with repository facts**

  Refactor the command handler into an injectable application service callable from tests. `diagnose` must report discovered source/test counts, `.slnx` parity, unresolved project references, retired references, missing lock files, and retired dependencies found inside locks. For authoritative staleness, invoke `dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode` as a child process and propagate a non-zero exit; do not attempt to infer arbitrary NuGet graph staleness from XML/text alone. Do not print `available/not detected/checked` without inspecting the repository.

- [ ] **Step 5: Add CLI theory tests**

  Cover every retired PackageId as both `ProjectReference` and `PackageReference`, case-insensitive matching, allowed target packages, layer violations, malformed XML, missing repository, and command exit codes.

- [ ] **Step 6: Verify architecture and CLI gates**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
  dotnet test backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj
  dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -- audit dependencies --repository .
  dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -- diagnose --repository .
  ```

  Expected: PASS/exit 0; exact inventories are 57 and 50, solution parity is green, and there are no retired references.

- [ ] **Step 7: Commit**

  ```powershell
  git add backend/dotnet/BuildingBlocks/tests/Architecture backend/dotnet/tools/src/Tw.Cli backend/dotnet/tools/tests/Tw.Cli.Tests docs/shared-packages/dotnet/Tw.Cli
  git commit -m "feat: lock consolidated package topology"
  ```

## Task 15: Repair templates, central package versioning, and lock files

**Files:**

- Modify: `backend/dotnet/Directory.Build.props`
- Create: `backend/dotnet/Directory.Build.targets`
- Modify: `backend/dotnet/Directory.Packages.props`
- Modify: `backend/dotnet/Build/Packages.ThirdParty.props`
- Modify: `backend/dotnet/Build/Packages.Framework.props`
- Modify: `backend/dotnet/Build/Packages.Internal.props`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/building-block/.template.config/template.json`
- Move: `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Tw.Sample/` -> `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Capability/Tw.Sample/`
- Move: `backend/dotnet/tools/src/Tw.Templates/content/building-block/tests/Tw.Sample.Tests/` -> `backend/dotnet/tools/src/Tw.Templates/content/building-block/tests/Capability/Tw.Sample.Tests/`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/building-block/src/Capability/Tw.Sample/package-charter.yaml`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/building-block/tests/Capability/Tw.Sample.Tests/Tw.Sample.Tests.csproj`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/building-block/README.md`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/Company.Gateway.Host.csproj`
- Modify: `backend/dotnet/tools/src/Tw.Templates/content/gateway/src/Company.Gateway.Host/packages.lock.json`
- Modify: `backend/dotnet/tools/tests/Tw.Templates.Tests/TemplateSmokeTests.cs`
- Create: `backend/dotnet/tools/scripts/Test-PackageConsumption.ps1`
- Create: `backend/dotnet/tools/scripts/Test-TemplateInstantiation.ps1`
- Modify: every retained `backend/dotnet/**/packages.lock.json`

- [ ] **Step 1: Add failing template tests**

  Extend `TemplateSmokeTests` to:

  - load retired PackageIds and `Tw.Interception` from `building-blocks-topology.json`, then scan every template `.csproj` and `packages.lock.json` for them plus Autofac/Castle;
  - resolve every conditional repository `ProjectReference` to a real path rather than compare a hard-coded string only;
  - verify the building-block template emits `src/<Capability>/<Package>` and `tests/<Capability>/<Package>.Tests`;
  - verify generated test projects reference their generated runtime project;
  - reject the placeholder responsibility and natural-language dependency patterns currently in the sample charter;
  - verify repository analyzer wiring explicitly excludes template-content projects so generated standalone templates never inherit a repository-only analyzer `ProjectReference`.

- [ ] **Step 2: Run template tests and confirm failure**

  ```powershell
  dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj
  ```

  Expected: FAIL because the building-block template still uses the old flat, placeholder shape and lacks a runtime-project reference; retired-package lock assertions may already pass because Tasks 3 and 12 refreshed the gateway lock.

- [ ] **Step 3: Establish one prerelease version input**

  Add `TwPackageVersion` with repository default `0.1.0-alpha.1` and allow CI to override it via `-p:TwPackageVersion=<version>`. Set `PackageVersion` from that property for packable .NET projects. Disable central floating versions. Remove the unused `GitVersion.MsBuild` central entry unless a real project imports it. Do not add internal `ProjectReference` packages mechanically to `Packages.Internal.props`; add only the gateway fallback identities actually evaluated under repository CPM (`Tw.Gateway`, `Tw.Gateway.Yarp`, `Tw.AspNetCore`, `Tw.Observability`, `Tw.Configuration`), all at `$(TwPackageVersion)`.

- [ ] **Step 4: Wire the brand analyzer before final lock generation**

  Put this analyzer reference in `backend/dotnet/Directory.Build.targets`:

  ```xml
  <ProjectReference Include="$(MSBuildThisFileDirectory)tools/src/Tw.Analyzers/Tw.Analyzers.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    PrivateAssets="all" />
  ```

  Apply it to repository .NET projects except `Tw.Analyzers`, `Tw.Analyzers.Tests`, and normalized paths below `tools/src/Tw.Templates/content`. Append `TWGOV001` to `WarningsAsErrors` without dropping `Nullable`. Build once, fix every real branded declaration by responsibility, and do not add production suppressions. This graph change deliberately precedes `--force-evaluate` lock generation.

  ```powershell
  dotnet build backend/dotnet/Tw.SmartPlatform.slnx -c Release
  ```

- [ ] **Step 5: Make the building-block template generate the governed shape**

  Add required `capability`, `owner`, `responsibility`, `inScope`, `outOfScope`, and `publicCapability` template parameters. Configure the `capability` symbol with `fileRename: "Capability"` so it renames both source/test path segments; use distinct sentinel replacements in YAML for the other values. Each of the last three supplies at least one truthful charter list entry; dependency rules use exact machine-matchable defaults (`*TestBase`/retired PackageIds where applicable), never natural-language placeholders. Generate:

  ```text
  src/<Capability>/<Package>/<Package>.csproj
  tests/<Capability>/<Package>.Tests/<Package>.Tests.csproj
  ```

  The test project must contain a relative `ProjectReference` to the generated runtime project. Generated charter `responsibility`, `in_scope`, `out_of_scope`, and exact dependency patterns must be usable values, not “生成后再修改”的 placeholders. Document that `tw-building-block` is generated at the `backend/dotnet/BuildingBlocks` root so it inherits repository central package management.

- [ ] **Step 6: Make standalone gateway package fallback versioned without violating CPM**

  Keep repository `ProjectReference` mode for source-tree smoke tests. Under central package management, emit fallback `PackageReference` items without `Version` and provide their versions from `Packages.Internal.props`; outside CPM, emit a mutually exclusive item group with `Version="$(TwFrameworkVersion)"`. Source `TwFrameworkVersion` from one template parameter/default. Test both evaluated branches so NU1008 cannot occur. Do not remove `Company.Service.Domain.Shared`; it is a bounded-context-local service project, not the retired global `Tw.Domain.Shared` package.

- [ ] **Step 7: Regenerate every retained and template-content lock file**

  After source references and central versions are final:

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --force-evaluate
  $templateProjects = Get-ChildItem backend/dotnet/tools/src/Tw.Templates/content -Recurse -Filter *.csproj
  foreach ($project in $templateProjects) {
    dotnet restore $project.FullName --force-evaluate -p:UseRepositoryProjectReferences=true
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  }
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
  foreach ($project in $templateProjects) {
    dotnet restore $project.FullName --locked-mode -p:UseRepositoryProjectReferences=true
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  }
  ```

  The solution restore covers retained BuildingBlocks and .NET tool projects; the explicit loop covers content projects that are packed as files and are not solution ProjectReferences. Never hand-edit dependency graphs in `packages.lock.json`. Repeat the solution and content-project loop with `--locked-mode` after generation.

- [ ] **Step 8: Implement package consume verification**

  `Test-PackageConsumption.ps1` must accept `-Version` and `-OutputDirectory`, load all 57 runtime paths from `building-blocks-topology.json`, pack them into `$OutputDirectory/feed`, verify one `.nupkg` per PackageId/version, inspect nuspec/lock dependency identities for retired packages, then create isolated minimal `net10.0` consumers that restore and build against each package using the local Tw feed plus only the approved external sources from `backend/dotnet/NuGet.Config`. It must stop on the first failure, create a unique child under `$OutputDirectory/runs`, validate the resolved child path remains under that directory before cleanup, and never delete the caller-owned output/feed directories.

- [ ] **Step 9: Implement isolated template installation verification**

  `Test-TemplateInstantiation.ps1` accepts `-TemplatePackage`, `-PackageSource`, and `-Version`. Install the packed template into a custom hive; instantiate `tw-service` and `tw-gateway` in script-owned temp directories, inject a temporary NuGet.Config/local feed and `TwFrameworkVersion`, then restore locked and build. Instantiate `tw-building-block` in a verified script-owned child below `backend/dotnet/BuildingBlocks/.template-smoke` so it inherits repository CPM, verify the exact source/test capability paths and project reference, then clean only that child. Propagate every non-zero process exit.

- [ ] **Step 10: Verify locked restore, templates, and packages**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
  dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj --no-restore
  & backend/dotnet/tools/scripts/Test-PackageConsumption.ps1 -Version 0.1.0-alpha.1 -OutputDirectory artifacts/package-consume
  dotnet pack backend/dotnet/tools/src/Tw.Templates/Tw.Templates.csproj -c Release --no-restore -o artifacts/templates -p:TwPackageVersion=0.1.0-alpha.1
  & backend/dotnet/tools/scripts/Test-TemplateInstantiation.ps1 `
    -TemplatePackage artifacts/templates/Tw.Templates.0.1.0-alpha.1.nupkg `
    -PackageSource artifacts/package-consume/feed `
    -Version 0.1.0-alpha.1
  ```

  Expected: PASS; no retained lock contains retired PackageIds, Autofac, or Castle.

- [ ] **Step 11: Commit**

  ```powershell
  git add backend/dotnet
  git commit -m "build: align templates versions and locked dependencies"
  ```

## Task 16: Repair charter discovery, docs parity, and generated memory

**Files:**

- Modify: `tools/src/tw_memory/repo.py`
- Modify: `tools/src/tw_memory/packages.py`
- Modify: `tools/src/tw_memory/check.py`
- Modify: `tools/src/tw_memory/implemented_api.py`
- Modify: `tools/tests/conftest.py`
- Modify: `tools/tests/test_packages.py`
- Modify: `tools/tests/test_generate.py`
- Modify: `tools/tests/test_check.py`
- Modify: `tools/tests/test_implemented_api.py`
- Modify: `tools/tests/test_cards.py`
- Modify: `.pre-commit-config.yaml`
- Modify: `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- Modify: `docs/engineering-standards/10-governance/dotnet-framework-governance.md`
- Modify: `docs/shared-packages/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`
- Create: `docs/shared-packages/dotnet/migrations/2026-07-building-blocks-consolidation.md`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Mvc.NewtonsoftJson/README.md`
- Create: `docs/shared-packages/dotnet/Tw.BackgroundJobs.Abstractions/README.md`
- Create: `docs/shared-packages/dotnet/Tw.BackgroundJobs.Quartz/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Caching.FusionCache/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Configuration.Nacos/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.DistributedLocking.Redis/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Excel/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Excel.MiniExcel/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.ExceptionHandling/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Gateway.Yarp/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Http/README.md`
- Create: `docs/shared-packages/dotnet/Tw.IdGeneration/README.md`
- Create: `docs/shared-packages/dotnet/Tw.IdGeneration.Yitter/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Json.Abstractions/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Json.Newtonsoft/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Security/README.md`
- Create: `docs/shared-packages/dotnet/Tw.TextTemplating/README.md`
- Create: `docs/shared-packages/dotnet/Tw.TextTemplating.Scriban/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Cli/README.md`
- Modify: `docs/shared-packages/dotnet/Tw.Templates/README.md`
- Modify: `.tw-memory/routes/packages.generated.yaml`
- Modify: `.tw-memory/routes/standards.generated.yaml`
- Modify: `.tw-memory/manifest/source-index.generated.json`
- Regenerate: `.tw-memory/cards/packages/*.generated.md`
- Regenerate: `.tw-memory/cards/public-apis/*.generated.md`

- [ ] **Step 1: Replace flat-layout Python fixtures and write failing discovery tests**

  Change the fixture signature to:

  ```python
  def make_csproj(root: Path, capability: str, name: str, body: str = "") -> Path:
  ```

  It must create `BuildingBlocks/src/<Capability>/<Name>/<Name>.csproj`. Add tests that discover a valid project, report (not silently ignore) `src/<Name>/<Name>.csproj` and mismatched directory/project stems, discover tool projects at `backend/dotnet/tools/src/<Tool>/<Tool>.csproj`, reject a canonical key duplicated between BuildingBlocks and tools, and prove `Tw.Templates/content/**/*.cs` is not collected as `Tw.Templates` public API.

- [ ] **Step 2: Run Python tests and confirm failure**

  ```powershell
  $env:PYTHONPATH = (Resolve-Path tools/src).Path
  $env:PYTHONDONTWRITEBYTECODE = "1"
  python -m pytest tools/tests
  ```

  Expected: FAIL because `glob("*/*.csproj")` discovers zero real BuildingBlocks and test fixtures use the obsolete shape.

- [ ] **Step 3: Implement strict discovery and exact-overlap semantics**

  Enumerate all candidate `.csproj` files under the two governed roots, classify only BuildingBlocks `Capability/Package/Package.csproj` and tools `Tool/Tool.csproj` as valid, and return shape diagnostics for every other non-template/non-bin/non-obj candidate so invalid projects cannot disappear from governance. Reject duplicate canonical keys across roots. Change `public_capabilities` conflict detection to normalized exact duplicate IDs only. `Tw.Data` and `Tw.Data.SqlSugar` are distinct IDs; semantic overlap remains a charter/review concern. In `implemented_api.py`, derive compile inputs from the project model: when `EnableDefaultItems`/`EnableDefaultCompileItems` is false, scan only explicit `Compile` items; therefore `Tw.Templates`, which packs `content/**` only as Content, contributes no template sample APIs.

- [ ] **Step 4: Make repository checks validate real facts**

  Require each discovered package to have `package-charter.yaml` and `docs/shared-packages/dotnet/<Package>/README.md`; load the approved BuildingBlocks/tool/retired inventory from `building-blocks-topology.json`, reject duplicate canonical keys and retired PackageIds, require top-level/dotnet/package indexes to have exact parity, reject orphan docs/cards/routes, and verify source-index paths/hashes. Treat `docs/shared-packages/dotnet/migrations` as the sole indexed non-package documentation directory: both migration files must be linked from the dotnet index, and the directory must never produce a package card. The final expected governed .NET inventory is 60: the manifest's 57 BuildingBlocks paths plus `Tw.Analyzers`, `Tw.Cli`, and `Tw.Templates`.

- [ ] **Step 5: Reconcile all charters and usage docs**

  Update retained charters during their owning migration tasks, then make a final exact-parity pass here. Provider READMEs must accurately say `experimental` and list missing stable gates; do not publish invented SDK examples. The migration document must map all 16 retired PackageIds to their target or deletion rationale, include API/namespace renames, test-project changes, and explicitly state that service-local `Company.Service.Domain.Shared` is unaffected.

- [ ] **Step 6: Correct formal governance commands and scope**

  Extend charter scope to the three packageable .NET tools. Replace the misleading “pytest charter gate” wording with:

  ```powershell
  $env:PYTHONPATH = (Resolve-Path tools/src).Path
  python -m pytest tools/tests
  python -m tw_memory check --root .
  ```

  Replace the environment-dependent system hook with an isolated Python hook:

  ```yaml
  - repo: local
    hooks:
      - id: tw-memory-check
        name: tw-memory check
        entry: python -m tw_memory check --staged
        language: python
        additional_dependencies:
          - ./tools
        pass_filenames: false
        always_run: true
  ```

  Verify `pre-commit run tw-memory-check --all-files` on the repository's Windows and Linux CI jobs; it must not depend on a developer-installed `tw-memory` or ambient `PYTHONPATH`.

- [ ] **Step 7: Generate memory from the repaired inventory**

  ```powershell
  $env:PYTHONPATH = (Resolve-Path tools/src).Path
  python -m tw_memory generate --root .
  python -m tw_memory check --root .
  ```

  Expected: PASS; 60 .NET package cards and 60 public API cards, correct three-level source paths, no `Tw.Localization.AspNetCore`, no retired card, and no orphan generated files.

- [ ] **Step 8: Run all Python tests**

  ```powershell
  python -m pytest tools/tests
  ```

  Expected: PASS.

- [ ] **Step 9: Commit**

  ```powershell
  git add tools .pre-commit-config.yaml docs/engineering-standards docs/shared-packages .tw-memory
  git commit -m "fix: align governance tools with package topology"
  ```

## Task 17: Run release-grade verification

**Files:**

- Modify: any remaining C# file reported by `TWGOV001`
- Modify: any remaining charter, doc, lock, solution, or generated file reported by final gates

- [ ] **Step 1: Prove the already-wired analyzer and locks are stable**

  ```powershell
  dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
  dotnet build backend/dotnet/Tw.SmartPlatform.slnx -c Release --no-restore
  ```

  Expected: PASS. Any missed branded declaration fails as `TWGOV001`; rename by responsibility and rerun without suppression. The only analyzer exemption is `Tw.Core` assembly's `Tw.Exceptions.TwException`.

- [ ] **Step 2: Run all focused governance gates**

  ```powershell
  dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj -c Release --no-restore
  dotnet test backend/dotnet/tools/tests/Tw.Analyzers.Tests/Tw.Analyzers.Tests.csproj -c Release --no-restore
  dotnet test backend/dotnet/tools/tests/Tw.Cli.Tests/Tw.Cli.Tests.csproj -c Release --no-restore
  dotnet test backend/dotnet/tools/tests/Tw.Templates.Tests/Tw.Templates.Tests.csproj -c Release --no-restore
  $env:PYTHONPATH = (Resolve-Path tools/src).Path
  python -m pytest tools/tests
  python -m tw_memory check --root .
  ```

  Expected: PASS.

- [ ] **Step 3: Run full .NET build and test**

  ```powershell
  dotnet build backend/dotnet/Tw.SmartPlatform.slnx -c Release --no-restore
  dotnet test backend/dotnet/Tw.SmartPlatform.slnx -c Release --no-build --no-restore
  ```

  Expected: PASS with 57 BuildingBlocks source projects and 50 tests; no missing/duplicate solution entries.

- [ ] **Step 4: Run dependency audit, template pack, and package consumption**

  ```powershell
  dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -c Release --no-build -- audit dependencies --repository .
  dotnet run --project backend/dotnet/tools/src/Tw.Cli/Tw.Cli.csproj -c Release --no-build -- diagnose --repository .
  & backend/dotnet/tools/scripts/Test-PackageConsumption.ps1 -Version 0.1.0-alpha.1 -OutputDirectory artifacts/package-consume
  dotnet pack backend/dotnet/tools/src/Tw.Templates/Tw.Templates.csproj -c Release --no-build -o artifacts/templates -p:TwPackageVersion=0.1.0-alpha.1
  & backend/dotnet/tools/scripts/Test-TemplateInstantiation.ps1 `
    -TemplatePackage artifacts/templates/Tw.Templates.0.1.0-alpha.1.nupkg `
    -PackageSource artifacts/package-consume/feed `
    -Version 0.1.0-alpha.1
  ```

  Expected: exit 0; all 57 packages restore and build from the local feed, and all three templates instantiate in their supported context.

- [ ] **Step 5: Review the final diff for accidental compatibility or user-worktree damage**

  ```powershell
  git status --short
  git diff --check
  git diff --stat
  ```

  Confirm no unrelated pre-existing work was staged, no old compatibility facade was introduced, and provider packages still marked experimental were not described as production complete.

- [ ] **Step 6: Commit**

  ```powershell
  git add backend/dotnet/Directory.Build.props backend/dotnet/Directory.Build.targets backend/dotnet/BuildingBlocks backend/dotnet/tools tools docs/engineering-standards docs/shared-packages .tw-memory .pre-commit-config.yaml
  git diff --cached --name-only
  git commit -m "refactor: complete building blocks package consolidation"
  ```

## Follow-up provider plans required before stable release

Create one independent spec/plan per provider or host boundary, each with real dependency tests, failure semantics, timeout/deadline, health, observability, operations and rollback: `Tw.BackgroundJobs.Quartz`, `Tw.Caching.FusionCache`, `Tw.Configuration.Nacos`, `Tw.Data.SqlSugar`, `Tw.DistributedLocking.Redis`, `Tw.EventBus.Cap`, `Tw.Gateway.Yarp`, `Tw.Observability.OpenTelemetry`, `Tw.AspNetCore.Grpc`, `Tw.AspNetCore.TestBase`, `Tw.Data.SqlSugar.TestBase`, and `Tw.EventBus.Cap.TestBase`. None of these follow-ups changes the 57-project topology without a new architecture review.

## Appendix A: Approved 57 BuildingBlocks source projects

Human-readable mirror of `backend/dotnet/BuildingBlocks/building-blocks-topology.json`; implementation gates read the manifest and tests assert this design inventory remains consistent.

```text
Application/Tw.Application/Tw.Application.csproj
Application/Tw.Application.Contracts/Tw.Application.Contracts.csproj
Application/Tw.Authorization/Tw.Authorization.csproj
Application/Tw.Domain/Tw.Domain.csproj
Application/Tw.Features/Tw.Features.csproj
Application/Tw.Identity.OpenIddict/Tw.Identity.OpenIddict.csproj
Application/Tw.Settings/Tw.Settings.csproj
Auditing/Tw.Auditing/Tw.Auditing.csproj
Auditing/Tw.Auditing.Contracts/Tw.Auditing.Contracts.csproj
BackgroundJobs/Tw.BackgroundJobs/Tw.BackgroundJobs.csproj
BackgroundJobs/Tw.BackgroundJobs.Abstractions/Tw.BackgroundJobs.Abstractions.csproj
BackgroundJobs/Tw.BackgroundJobs.Quartz/Tw.BackgroundJobs.Quartz.csproj
Caching/Tw.Caching/Tw.Caching.csproj
Caching/Tw.Caching.FusionCache/Tw.Caching.FusionCache.csproj
Configuration/Tw.Configuration/Tw.Configuration.csproj
Configuration/Tw.Configuration.Nacos/Tw.Configuration.Nacos.csproj
Data/Tw.Data/Tw.Data.csproj
Data/Tw.Data.SqlSugar/Tw.Data.SqlSugar.csproj
DistributedLocking/Tw.DistributedLocking/Tw.DistributedLocking.csproj
DistributedLocking/Tw.DistributedLocking.Redis/Tw.DistributedLocking.Redis.csproj
EventBus/Tw.EventBus/Tw.EventBus.csproj
EventBus/Tw.EventBus.Cap/Tw.EventBus.Cap.csproj
Excel/Tw.Excel/Tw.Excel.csproj
Excel/Tw.Excel.MiniExcel/Tw.Excel.MiniExcel.csproj
Foundation/Tw.Core/Tw.Core.csproj
Foundation/Tw.DependencyInjection/Tw.DependencyInjection.csproj
Foundation/Tw.DependencyInjection.Abstractions/Tw.DependencyInjection.Abstractions.csproj
Foundation/Tw.ExceptionHandling/Tw.ExceptionHandling.csproj
Foundation/Tw.Json.Abstractions/Tw.Json.Abstractions.csproj
Foundation/Tw.Json.Newtonsoft/Tw.Json.Newtonsoft.csproj
Foundation/Tw.Security/Tw.Security.csproj
Gateway/Tw.Gateway/Tw.Gateway.csproj
Gateway/Tw.Gateway.Yarp/Tw.Gateway.Yarp.csproj
Grpc/Tw.Grpc/Tw.Grpc.csproj
Http/Tw.Http/Tw.Http.csproj
Idempotency/Tw.Idempotency/Tw.Idempotency.csproj
IdGeneration/Tw.IdGeneration/Tw.IdGeneration.csproj
IdGeneration/Tw.IdGeneration.Yitter/Tw.IdGeneration.Yitter.csproj
Localization/Tw.Localization/Tw.Localization.csproj
MultiTenancy/Tw.MultiTenancy/Tw.MultiTenancy.csproj
Observability/Tw.Observability/Tw.Observability.csproj
Observability/Tw.Observability.OpenTelemetry/Tw.Observability.OpenTelemetry.csproj
Observability/Tw.Observability.Serilog/Tw.Observability.Serilog.csproj
Resilience/Tw.Resilience/Tw.Resilience.csproj
Sharding/Tw.Sharding/Tw.Sharding.csproj
TestBase/Tw.AspNetCore.TestBase/Tw.AspNetCore.TestBase.csproj
TestBase/Tw.Data.SqlSugar.TestBase/Tw.Data.SqlSugar.TestBase.csproj
TestBase/Tw.EventBus.Cap.TestBase/Tw.EventBus.Cap.TestBase.csproj
TestBase/Tw.TestBase/Tw.TestBase.csproj
TextTemplating/Tw.TextTemplating/Tw.TextTemplating.csproj
TextTemplating/Tw.TextTemplating.Scriban/Tw.TextTemplating.Scriban.csproj
Web/Tw.AspNetCore/Tw.AspNetCore.csproj
Web/Tw.AspNetCore.Grpc/Tw.AspNetCore.Grpc.csproj
Web/Tw.AspNetCore.Localization/Tw.AspNetCore.Localization.csproj
Web/Tw.AspNetCore.Mvc/Tw.AspNetCore.Mvc.csproj
Web/Tw.AspNetCore.Mvc.NewtonsoftJson/Tw.AspNetCore.Mvc.NewtonsoftJson.csproj
Web/Tw.AspNetCore.Swashbuckle/Tw.AspNetCore.Swashbuckle.csproj
```

## Appendix B: Approved 50 BuildingBlocks test projects

Human-readable mirror of `backend/dotnet/BuildingBlocks/building-blocks-topology.json`.

```text
Application/Tw.Application.Contracts.Tests/Tw.Application.Contracts.Tests.csproj
Application/Tw.Application.Tests/Tw.Application.Tests.csproj
Application/Tw.Authorization.Tests/Tw.Authorization.Tests.csproj
Application/Tw.Domain.Tests/Tw.Domain.Tests.csproj
Application/Tw.Features.Tests/Tw.Features.Tests.csproj
Application/Tw.Identity.OpenIddict.Tests/Tw.Identity.OpenIddict.Tests.csproj
Application/Tw.Settings.Tests/Tw.Settings.Tests.csproj
Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
Auditing/Tw.Auditing.Contracts.Tests/Tw.Auditing.Contracts.Tests.csproj
Auditing/Tw.Auditing.Tests/Tw.Auditing.Tests.csproj
BackgroundJobs/Tw.BackgroundJobs.Quartz.Tests/Tw.BackgroundJobs.Quartz.Tests.csproj
BackgroundJobs/Tw.BackgroundJobs.Tests/Tw.BackgroundJobs.Tests.csproj
Caching/Tw.Caching.Tests/Tw.Caching.Tests.csproj
Configuration/Tw.Configuration.Tests/Tw.Configuration.Tests.csproj
Data/Tw.Data.SqlSugar.Tests/Tw.Data.SqlSugar.Tests.csproj
Data/Tw.Data.Tests/Tw.Data.Tests.csproj
DistributedLocking/Tw.DistributedLocking.Tests/Tw.DistributedLocking.Tests.csproj
EventBus/Tw.EventBus.Cap.Tests/Tw.EventBus.Cap.Tests.csproj
EventBus/Tw.EventBus.Tests/Tw.EventBus.Tests.csproj
Excel/Tw.Excel.MiniExcel.Tests/Tw.Excel.MiniExcel.Tests.csproj
Excel/Tw.Excel.Tests/Tw.Excel.Tests.csproj
Foundation/Tw.Core.Tests/Tw.Core.Tests.csproj
Foundation/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj
Foundation/Tw.DependencyInjection.Tests.Fixtures/Tw.DependencyInjection.Tests.Fixtures.csproj
Foundation/Tw.ExceptionHandling.Tests/Tw.ExceptionHandling.Tests.csproj
Foundation/Tw.Json.Newtonsoft.Tests/Tw.Json.Newtonsoft.Tests.csproj
Foundation/Tw.Security.Tests/Tw.Security.Tests.csproj
Gateway/Tw.Gateway.Tests/Tw.Gateway.Tests.csproj
Gateway/Tw.Gateway.Yarp.Tests/Tw.Gateway.Yarp.Tests.csproj
Grpc/Tw.Grpc.Tests/Tw.Grpc.Tests.csproj
Http/Tw.Http.Tests/Tw.Http.Tests.csproj
Idempotency/Tw.Idempotency.Tests/Tw.Idempotency.Tests.csproj
IdGeneration/Tw.IdGeneration.Tests/Tw.IdGeneration.Tests.csproj
IdGeneration/Tw.IdGeneration.Yitter.Tests/Tw.IdGeneration.Yitter.Tests.csproj
Localization/Tw.Localization.Tests/Tw.Localization.Tests.csproj
MultiTenancy/Tw.MultiTenancy.Tests/Tw.MultiTenancy.Tests.csproj
Observability/Tw.Observability.Tests/Tw.Observability.Tests.csproj
Observability/Tw.Observability.OpenTelemetry.Tests/Tw.Observability.OpenTelemetry.Tests.csproj
Observability/Tw.Observability.Serilog.Tests/Tw.Observability.Serilog.Tests.csproj
Resilience/Tw.Resilience.Tests/Tw.Resilience.Tests.csproj
Sharding/Tw.Sharding.Tests/Tw.Sharding.Tests.csproj
TestBase/Tw.TestBase.Tests/Tw.TestBase.Tests.csproj
TextTemplating/Tw.TextTemplating.Tests/Tw.TextTemplating.Tests.csproj
TextTemplating/Tw.TextTemplating.Scriban.Tests/Tw.TextTemplating.Scriban.Tests.csproj
Web/Tw.AspNetCore.Grpc.Tests/Tw.AspNetCore.Grpc.Tests.csproj
Web/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj
Web/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj
Web/Tw.AspNetCore.Mvc.NewtonsoftJson.Tests/Tw.AspNetCore.Mvc.NewtonsoftJson.Tests.csproj
Web/Tw.AspNetCore.Swashbuckle.Tests/Tw.AspNetCore.Swashbuckle.Tests.csproj
Web/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
```
