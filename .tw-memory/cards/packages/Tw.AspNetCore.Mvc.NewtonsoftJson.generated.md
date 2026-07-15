# Package: Tw.AspNetCore.Mvc.NewtonsoftJson

标识：Tw.AspNetCore.Mvc.NewtonsoftJson / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc.NewtonsoftJson / platform-team
职责：提供 Newtonsoft.Json MVC 集成辅助能力，包括 long id 十进制字符串转换。

适用范围：
- long id JSON 转换器
- Newtonsoft.Json MVC 支持辅助能力

不适用范围：
- System.Text.Json 转换器
- OpenAPI schema 过滤器

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Newtonsoft.Json, Microsoft.AspNetCore.Mvc.NewtonsoftJson

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.Mvc.NewtonsoftJson
