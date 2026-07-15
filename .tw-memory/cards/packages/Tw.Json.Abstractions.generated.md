# Package: Tw.Json.Abstractions

标识：Tw.Json.Abstractions / backend/dotnet/BuildingBlocks/src/Foundation/Tw.Json.Abstractions / platform-team
职责：JSON 序列化与反序列化的框架级抽象契约。

适用范围：
- JSON 序列化接口
- JSON 反序列化接口
- 通用序列化选项模型

不适用范围：
- 具体 JSON 库实现
- HTTP JSON formatter
- OpenAPI Schema 生成

依赖边界：
- forbid: Microsoft.AspNetCore.*, Newtonsoft.Json, System.Text.Json
- allow: none

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Json.Abstractions
