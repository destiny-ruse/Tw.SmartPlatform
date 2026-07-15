# Package: Tw.AspNetCore.Swashbuckle

标识：Tw.AspNetCore.Swashbuckle / backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Swashbuckle / platform-team
职责：提供 Swashbuckle 注册、Newtonsoft 支持、JWT 操作元数据、稳定错误响应元数据、XML 注释与 long id 字符串 schema 映射。

适用范围：
- SwaggerGen 注册
- Newtonsoft 支持
- JWT 操作过滤器
- 统一错误响应操作过滤器
- long id schema 过滤器

不适用范围：
- MVC 模型绑定
- 运行时认证
- 生成客户端发布

依赖边界：
- forbid: SqlSugar*, DotNetCore.CAP*
- allow: Swashbuckle.AspNetCore, Swashbuckle.AspNetCore.Newtonsoft

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.AspNetCore.Swashbuckle
