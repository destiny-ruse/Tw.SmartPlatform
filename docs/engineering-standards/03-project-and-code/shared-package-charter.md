# 共享包 charter 规范

## 目标

统一共享包职责与边界的声明形式，使开发、测试、评审和包 owner 能判断能力归属、公开能力、依赖边界和兼容性承诺。

## 适用范围

适用于 `backend/dotnet/BuildingBlocks/src` 下的 .NET 公共构建块，以及 `frontend/packages` 下的前端共享包。

## 规范要求

- 每个共享包根目录必须包含 `package-charter.yaml`。
- charter 必须包含 `schema_version`、`package`、`owner`、`responsibility`、`in_scope`、`out_of_scope`、`public_capabilities`、`dependency_rules`。
- `in_scope`、`out_of_scope`、`public_capabilities` 必须为非空列表。
- `out_of_scope` 必须声明本包不承担的能力边界。
- `.NET` 包的 `package` 必须等于 `.csproj` 文件名去扩展名。
- 前端共享包的 `package` 必须等于 `package.json` 的 `name`。
- `dependency_rules.forbid` 声明禁止依赖；`dependency_rules.allow` 非空时声明允许依赖。
- `stability` 取值为 `experimental`、`stable`、`deprecated`，缺省为 `stable`。
- `compatibility` 用短文本声明兼容性承诺。
- `migration_ref` 指向仓库内 CHANGELOG、迁移说明或契约版本。

## 采纳前破坏性变更

共享包处于采纳前阶段时，允许直接对公开 API 做破坏性变更，包括删除或重命名公开类型与方法、迁移命名空间、调整服务注册入口，不要求保留废弃转发壳或兼容别名。

采纳前阶段同时满足以下两个条件：

- 该包未被任何具体微服务或应用项目引用。
- 该包未发布为 NuGet 包或其他外部制品。

任一条件不再满足时，该包退出采纳前阶段，破坏性变更必须遵循 `compatibility` 承诺，并通过 `migration_ref` 指向的迁移说明对外沟通。

采纳前阶段的破坏性整改必须同步更新受影响的 `package-charter.yaml` 与 `docs/shared-packages` 使用文档。

## 新增包流程

- 新能力落在已有包 `in_scope` 时进入该包。
- 新能力命中某包 `out_of_scope` 时不得放入该包。
- 建立新包必须同时满足单一职责清晰、存在跨服务或跨应用复用、依赖边界独立、公开能力不与现有包重叠。
- 建立新包必须同时提交 `package-charter.yaml`。

## 重叠处理

- `public_capabilities` 命名空间重叠必须重新划分。
- 职责语义重叠必须在代码评审中裁决，处理结论必须反映到相关包 charter。

## 能力使用文档

- 新增或修改共享包公开能力时，必须同步创建或更新 `docs/shared-packages/<language>/<package>/<feature>.md` 使用文档。
- 必须同步更新 `docs/shared-packages/README.md`、`docs/shared-packages/<language>/README.md` 和 `docs/shared-packages/<language>/<package>/README.md` 索引，保证从总索引可跳转到能力使用文档。
- 索引页采用 Reference 文档类型，能力使用文档采用 How-to Guide 文档类型。
- 能力使用文档必须覆盖能力定位、DI 注册方式、各入口使用方式和注意事项。

## 检查清单

- 共享包是否包含 `package-charter.yaml`？
- `out_of_scope` 是否非空？
- `package` 是否等于 canonical key？
- 实际依赖是否符合 `dependency_rules`？
- `public_capabilities` 是否与其他共享包互斥？
- 公开能力变更是否同步更新 `docs/shared-packages` 使用文档与索引？
- 破坏性变更前是否已判定该包处于采纳前阶段，或已按 `compatibility` 承诺处理？
