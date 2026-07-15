# Package: Tw.Json.Newtonsoft

标识：Tw.Json.Newtonsoft / backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Newtonsoft / platform-team
职责：基于 Newtonsoft.Json 的 JSON 序列化适配实现。

适用范围：
- Newtonsoft JSON 序列化器
- 长整型安全序列化规则
- 基础反序列化适配

不适用范围：
- System.Text.Json 实现
- ASP.NET Core MVC formatter
- API 文档生成

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: Tw.Json.Abstractions, Newtonsoft.Json

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Json.Newtonsoft
