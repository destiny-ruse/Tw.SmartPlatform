# Package: Tw.AspNetCore.TestBase

标识：Tw.AspNetCore.TestBase / backend/dotnet/BuildingBlocks/src/TestBase/Tw.AspNetCore.TestBase / platform-team
职责：提供 ASP.NET Core 测试辅助能力，生产项目不得引用该包。

适用范围：
- 带认证的 WebApplicationFactory
- 测试认证处理器

不适用范围：
- 生产认证

依赖边界：
- forbid: 生产项目引用
- allow: Tw.TestBase, Microsoft.AspNetCore.Mvc.Testing

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.TestBase
