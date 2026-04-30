# gRPC Proto 风格规范

## 目标

gRPC Proto 风格规范用于降低 proto 包名、服务、消息和字段风格不一致导致的生成代码冲突和契约误读风险。Proto 文件必须能被多语言稳定生成、审查和演进。统一风格也能让版本管理、契约测试和数据格式规范在 RPC 边界保持一致。

## 适用范围

本规范适用于 `proto/`、`apis/`、`services/`、`src/` 中 `.proto` 文件、gRPC 服务、消息、枚举、字段、生成代码配置、客户端 SDK 和契约测试。它不适用于 REST JSON、AsyncAPI 消息或进程内 DTO；这些对象只有在映射到 proto 契约时受本规范约束。

## 规则

1. `.proto` 文件必须使用 `proto3`，声明稳定 `package` 和目标语言选项；包名必须包含领域和版本，例如 `orders.v1`。
2. 服务名必须使用 `PascalCase` 并以业务能力命名；RPC 方法必须使用动词短语，清晰表达动作和资源。
3. 请求和响应消息必须独立命名，例如 `GetOrderRequest`、`GetOrderResponse`；不得复用内部数据库实体作为公开消息。
4. 字段名必须使用 `snake_case`，字段号一旦发布不得复用或改变语义；删除字段必须使用 `reserved` 保留字段号和字段名。
5. 枚举值必须包含明确默认值，使用稳定大写下划线命名；不得将显示文案作为枚举值。
6. 时间、金额、ID、布尔值和可选字段必须遵守数据格式规范，并使用适合 proto 的标准类型或明确消息结构。
7. 对外契约不得暴露内部异常、调试字段、数据库列名或安全敏感字段。
8. Proto 变更必须配套生成代码、契约测试和版本影响说明。

## 示例

正例：

```proto
syntax = "proto3";

package orders.v1;

service OrderService {
  rpc GetOrder(GetOrderRequest) returns (GetOrderResponse);
}

message GetOrderRequest {
  string order_id = 1;
}
```

反例：

```proto
message order_table {
  int64 id = 1;
  string status_text = 2;
  string db_debug = 3;
}
```

## 检查清单

- `.proto` 是否声明 `proto3`、稳定包名、版本和目标语言选项。
- 服务、方法、消息、字段和枚举命名是否符合 proto 风格。
- 字段号是否未被复用，删除字段是否使用 `reserved`。
- 时间、金额、ID 和枚举是否符合数据格式规范。
- 生成代码和契约测试是否随 proto 变更更新。

## 相关规范

- rules.grpc-versioning
- rules.data-formats
- rules.contract-testing

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
