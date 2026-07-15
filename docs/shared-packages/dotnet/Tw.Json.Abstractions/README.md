# Tw.Json.Abstractions

`Tw.Json.Abstractions` 定义非协议 JSON 的 provider-neutral 契约。

## 公开能力

- `IJsonSerializer`
- `JsonSerializerOptions`

## 稳定性与边界

本包处于 `experimental` 阶段。HTTP、OpenAPI、事件和持久 JSON 的版本契约由各自边界负责；本包不得公开 Newtonsoft.Json 或 System.Text.Json 类型。稳定前必须冻结命名、空值、枚举、日期、long 和错误语义。
