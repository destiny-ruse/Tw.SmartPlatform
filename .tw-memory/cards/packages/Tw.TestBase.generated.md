# Package: Tw.TestBase

标识：Tw.TestBase / backend/dotnet/BuildingBlocks/src/TestBase/Tw.TestBase / platform-team
职责：提供通用测试辅助能力与确定性测试原语，生产项目不得引用该包。

适用范围：
- 测试时钟
- 测试当前用户
- 测试当前租户
- 契约 JSON 选项

不适用范围：
- 生产运行时行为

依赖边界：
- forbid: 生产项目引用
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.TestBase
