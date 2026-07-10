# XML 文档注释整改设计

## 背景

`backend/dotnet` 下存在大量模板化 XML 文档注释，例如“执行 X 操作”“X 参数”“X 的执行结果”“表示 X 声明”。这些注释只复述标识符或语法动作，不能解释类型、成员、参数、返回值、异常和副作用的具体契约。当前还存在一行式 `<summary>`，与用户期望的多行 DocFX XML 注释格式不一致。

本次同时调整本地化默认配置：`LocalizationOptions.DefaultCulture` 的默认值从 `"en-US"` 改为简体中文 BCP 47 名称 `"zh-Hans"`，并同步更新注释和测试。

## 目标

- 全量整改 `backend/dotnet` 下人工维护 `.cs` 文件中的无意义模板化 XML 注释
- 统一 `.NET` XML `<summary>` 为多行格式
- 将禁止模板化注释和禁止一行式 `<summary>` 写入正式工程规范
- 增加可执行架构测试，阻止同类注释再次进入仓库
- 将 `LocalizationOptions.DefaultCulture` 默认值改为 `"zh-Hans"` 并验证行为

## 范围

纳入范围：

- `backend/dotnet/BuildingBlocks/src/**/*.cs`
- `backend/dotnet/BuildingBlocks/tests/**/*.cs`
- `backend/dotnet/tools/src/**/*.cs`
- `backend/dotnet/tools/tests/**/*.cs`
- `backend/dotnet/tools/src/Tw.Templates/content/**/*.cs`

排除范围：

- `bin`、`obj`、测试结果、构建输出和本地缓存
- 工具明确生成且不应人工维护的文件
- 非 C# 文件

## 规范变更设计

在 `docs/engineering-standards/03-project-and-code/coding-standards.md` 的注释规则中补充明确约束：

- 注释必须解释职责、意图、约束、风险、契约或失败语义
- 文档注释不得使用“执行 X 操作”“X 参数”“X 的执行结果”“表示 X 声明”“表示 X 字段”“表示 X 属性”等模板化句式
- 参数、返回值和属性注释不得只重复标识符

在 `docs/engineering-standards/03-project-and-code/language-specific/dotnet-core.md` 的 DocFX XML 文档注释章节中补充明确约束：

- `<summary>` 必须使用多行 XML 注释格式
- 不得使用 `/// <summary>...</summary>` 一行式格式
- 示例代码使用多行 `<summary>`，并保留现有 `<param>`、`<returns>`、`<exception>` 标签要求

## 代码整改设计

整改流程按文件分批执行：

1. 使用搜索定位模板化 XML 注释、一行式 `<summary>` 和 `DefaultCulture`
2. 对生产源码、测试源码、工具源码和模板源码逐文件改写注释
3. 保留 XML 标签结构，只替换自然语言内容和 `<summary>` 排版
4. 方法注释描述具体能力、返回语义、取消语义、异常语义和副作用
5. 类型、属性和字段注释描述具体职责或承载的数据含义
6. 测试方法注释描述被验证的行为，不复述测试方法名

注释改写不改变方法签名、访问级别、命名空间、运行逻辑和依赖关系。只有 `LocalizationOptions.DefaultCulture` 默认值属于行为变更。

## 本地化行为设计

`LocalizationOptions.DefaultCulture` 改为：

```csharp
public string DefaultCulture { get; set; } = "zh-Hans";
```

该属性注释同步说明默认文化为 `"zh-Hans"`。本地化测试增加默认值断言，验证新建 `LocalizationOptions` 实例默认使用简体中文 BCP 47 名称。现有显式配置 `"en-US"` 的测试保持显式配置，继续覆盖自定义默认文化和回退行为。

## 测试设计

新增或扩展架构测试，扫描 `backend/dotnet` 下人工维护的 `.cs` 文件：

- 检测并拒绝一行式 `/// <summary>...</summary>`
- 检测并拒绝“执行 X 操作”“X 参数”“X 的执行结果”“表示 X 声明”“表示 X 字段”“表示 X 属性”等模板化 XML 注释
- 排除构建输出、本地缓存和非人工维护文件
- 失败信息输出文件路径、行号和命中的注释内容，便于定位修复

本地化测试补充 `LocalizationOptions` 默认文化断言。执行验证时优先运行架构测试和本地化测试，再根据改动范围运行 `backend/dotnet` 相关测试或构建检查。

## 风险与控制

主要风险是批量注释改写范围大，容易误改代码或引入不准确契约。控制方式：

- 每次只改 XML 注释和明确的 `DefaultCulture` 默认值
- 使用搜索结果驱动整改，避免无关重构
- 通过架构测试确认模板化句式已清零
- 通过本地化测试确认默认文化行为
- 通过 `git diff` 审核是否存在非预期代码逻辑变更

## 验收标准

- `backend/dotnet` 人工维护 `.cs` 文件中不存在一行式 `<summary>`
- `backend/dotnet` 人工维护 `.cs` 文件中不存在已列明的模板化 XML 注释句式
- 正式工程规范包含禁止模板化注释和禁止一行式 `.NET` `<summary>` 的规则
- 架构测试能够在重新引入模板注释或一行式 `<summary>` 时失败
- `LocalizationOptions.DefaultCulture` 默认值为 `"zh-Hans"`
- 本地化相关测试和 XML 注释架构测试通过
