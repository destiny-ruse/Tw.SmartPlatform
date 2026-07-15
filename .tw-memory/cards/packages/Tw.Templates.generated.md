# Package: Tw.Templates

标识：Tw.Templates / backend/dotnet/tools/src/Tw.Templates / dotnet-framework
职责：提供 service、gateway、building-block 和 contract-package 的官方 `dotnet new` 模板。

适用范围：
- `tw-service` 模板
- `tw-gateway` 模板
- `tw-building-block` 模板
- `tw-contract-package` 模板

不适用范围：
- 模板生成后的业务功能实现
- 运行时框架包兼容壳

依赖边界：
- forbid: runtime framework packages, compatibility aliases
- allow: Microsoft.NET.Sdk

稳定性：experimental
兼容性：模板短名称和输出目录结构作为项目创建契约保持稳定。
迁移指针：

source_refs:
- charter:package-charter:Tw.Templates
