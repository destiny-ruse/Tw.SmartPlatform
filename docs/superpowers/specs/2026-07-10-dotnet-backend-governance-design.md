# backend/dotnet 治理重整设计

## 背景

`backend/dotnet` 已包含一批公共构建块、工具项目、架构测试、中央包版本配置和治理脚本。当前结构存在以下问题：

- `BuildingBlocks/tests` 与 `BuildingBlocks/src` 的能力目录没有对齐
- `backend/dotnet/tools` 源码项目与测试项目平铺在同一层级
- `*.Abstractions.Tests` 违背“Abstractions 包只承载基础抽象契约，不单独维护测试项目”的边界
- `Build` 目录同时承载 `.props`、质量门禁脚本和占位 build runner
- `package-charter.yaml` 的 schema 和自然语言内容不统一
- 注释规则尚未覆盖私有、内部方法、字段和属性
- 解决方案中存在空壳文件夹，本地构建输出目录也混入治理视野

## 目标

本次治理建立可验证的目录、注释、charter、构建辅助和质量门禁边界，使后端公共包按包职责维护，并通过自动化检查防止结构回退。

## 非目标

- 不删除 `*.Abstractions` 生产项目包
- 不因为生产项目当前源码较少而删除该项目包
- 不为补齐注释重构业务逻辑
- 不保留只打印成功结果的质量门禁脚本

## 结构设计

`BuildingBlocks/tests` 按 `BuildingBlocks/src` 的能力目录镜像组织：

- `BuildingBlocks/tests/Foundation/Tw.Core.Tests`
- `BuildingBlocks/tests/Application/Tw.Application.Tests`
- `BuildingBlocks/tests/Web/Tw.AspNetCore.Tests`
- `BuildingBlocks/tests/Architecture/Tw.Architecture.Tests`

`Tw.Architecture.Tests` 放在 `Architecture` 能力目录下，因为它验证整体治理规则，不对应单个生产包。

所有 `*.Abstractions.Tests` 测试项目从解决方案和文件系统中删除。对应的 `*.Abstractions` 生产项目包保留，继续作为基础抽象契约包存在。

`backend/dotnet/tools` 拆分为源码与测试目录：

- `backend/dotnet/tools/src/Tw.Analyzers`
- `backend/dotnet/tools/src/Tw.Cli`
- `backend/dotnet/tools/src/Tw.Templates`
- `backend/dotnet/tools/tests/Tw.Analyzers.Tests`
- `backend/dotnet/tools/tests/Tw.Cli.Tests`
- `backend/dotnet/tools/tests/Tw.Templates.Tests`

清理范围只覆盖没有工程文件、没有正式源码、没有正式配置或文档价值的空壳目录，包括仅包含 `bin`、`obj` 的目录和完全空目录。不会删除有 `.csproj`、`package-charter.yaml` 或正式模板内容的项目目录。

## 规范设计

注释规则写入 `docs/engineering-standards/03-project-and-code/coding-standards.md`：

- 人工维护的类型、方法、构造函数、属性、字段和事件必须具备文档注释
- 规则覆盖 `public`、`internal`、`private`、`protected` 成员
- 方法体内部注释仍只用于解释复杂逻辑、风险、兼容性、边界处理和非显而易见的取舍

.NET 专项规则写入 `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md`：

- XML 文档注释范围覆盖人工维护成员
- 私有和内部成员同样使用符合 DocFX 处理习惯的 XML 注释格式
- 测试源码遵守相同自然语言注释规则

charter 语言规则写入 `docs/engineering-standards/03-project-and-code/shared-package-charter.md`：

- `responsibility`、`in_scope`、`out_of_scope`、`compatibility` 中的自然语言必须使用简体中文
- 包名、命名空间、依赖名、命令名、错误码、协议名保持原文
- `public_capabilities` 允许使用命名空间和能力标识原文

## Build 与质量门禁设计

`backend/dotnet/Build` 的目标职责收敛为中央包版本和构建级 MSBuild 配置：

- 保留 `Packages.*.props`
- 保留必要的 `packages.lock.json`
- 删除 `Build.csproj` 和 `Build.cs` 占位 runner

`Build/QualityGates` 不作为目录保留。现有脚本按实际价值处理：

- 删除仅打印成功结果的占位脚本：`CapEventContractGuard.ps1`、`ContractCompatibilityGuard.ps1`、`CoverageThresholdGuard.ps1`、`ErrorCodeCatalogGuard.ps1`、`LongIdContractGuard.ps1`
- 将有实际规则价值的检查迁入 `Tw.Architecture.Tests` 或 `Tw.Cli` 治理命令
- 更新 `docs/engineering-standards/10-governance/dotnet-framework-governance.md`，移除无效脚本命令，记录真实可执行的验证命令

现有有价值规则包括：

- package charter 存在性与 schema 检查
- 生产项目不得引用 `*TestBase`
- 网关包边界检查
- 禁用旧包名检查
- 敏感输出扫描

## charter 与工具校验设计

`tools/src/tw_memory/charter.py` 已参与 `package-charter.yaml` 校验。实现阶段同步加入中文内容规则，并补齐 `tools/tests/test_charter.py` 测试。

`backend/dotnet` 下的工具包 charter 统一迁移到正式 schema：

- `schema_version`
- `package`
- `owner`
- `responsibility`
- `in_scope`
- `out_of_scope`
- `public_capabilities`
- `dependency_rules`
- `stability`
- `compatibility`

工具模板中的示例 charter 也同步输出正式 schema 和中文自然语言内容。

## 架构测试设计

`Tw.Architecture.Tests` 补齐以下治理测试：

- 生产项目位于 `BuildingBlocks/src/<Capability>/<Package>/<Package>.csproj`
- 测试项目位于 `BuildingBlocks/tests/<Capability>/<TestProject>/<TestProject>.csproj`
- 每个非 `Abstractions` 生产包的测试项目路径与源码能力目录一致
- 不存在 `*.Abstractions.Tests`
- `backend/dotnet/tools` 只使用 `src` 与 `tests` 两个工程承载目录
- `Build` 下不存在非 `.props` 的占位构建项目和质量门禁脚本目录
- `package-charter.yaml` 满足正式 schema 和中文内容规则

## 注释补齐策略

注释补齐按能力目录分批执行：

1. `Foundation`
2. `Application`
3. `Web`
4. 其他 `BuildingBlocks/src` 能力目录
5. `backend/dotnet/tools/src`
6. 测试辅助类型和架构测试

补齐时只描述职责、参数、返回值、异常语义、边界和副作用。注释不得复述语法动作，不使用“获取”“设置”“获取或设置”等模板化属性说明。

发现类、方法、字段或属性没有引用且不属于公开能力、测试夹具、配置模型、序列化模型或模板输出时删除。删除前通过搜索、项目引用和测试验证确认影响边界。

## 验证设计

实现按红绿闭环推进：

1. 先新增或调整架构测试，使现状违反规则的用例失败
2. 完成目录迁移、测试删除、tools 拆分、Build 清理和 charter 规则更新
3. 运行 `dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj`
4. 运行 Python charter 校验相关测试
5. 运行受影响的 .NET 测试项目
6. 运行 `dotnet test backend/dotnet/Tw.SmartPlatform.slnx`

全量验证失败时，记录失败项目、失败命令、失败原因、已通过命令和剩余风险。

## 风险与控制

- 目录迁移会影响 `.slnx`、`ProjectReference` 和 lock 文件路径，迁移后必须统一更新
- 删除 `*.Abstractions.Tests` 会减少抽象包直接测试覆盖，相关行为通过使用方包测试和架构测试承接
- 注释补齐范围大，按能力目录分批提交并执行局部测试
- 质量门禁脚本删除后，必须用架构测试或 CLI 命令承接真实规则，避免治理能力丢失
