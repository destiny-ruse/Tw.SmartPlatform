# Tw.AspNetCore.Mvc.NewtonsoftJson

`Tw.AspNetCore.Mvc.NewtonsoftJson` 是选择 Newtonsoft.Json 的 MVC 兼容包，公开 `LongIdJsonConverter`，将 `long` 与 `long?` 按十进制字符串读写。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须完成真实 MVC 序列化集成、HTTP/OpenAPI long ID 契约一致性和错误输入行为验证。

## 边界

- 包内能力只适用于 Newtonsoft.Json MVC 边界
- System.Text.Json 转换器和 OpenAPI schema 过滤器不属于本包
- 引用本包不会自动修改 MVC 配置，宿主负责在组合根选择序列化 provider
