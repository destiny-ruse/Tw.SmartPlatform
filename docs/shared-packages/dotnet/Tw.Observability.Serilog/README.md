# Tw.Observability.Serilog

`Tw.Observability.Serilog` 为 Serilog 注册结构化敏感属性脱敏器，并使用 `Tw.Security.DataMasking.IDataMasker` 替换敏感标量值。

## 注册敏感属性脱敏

在创建 logger 前把脱敏器加入 `LoggerConfiguration`：

```csharp
using Serilog;
using Tw.Observability.Serilog;
using Tw.Security.DataMasking;

using var logger = new LoggerConfiguration()
    .EnrichWithSensitiveDataRedaction(DefaultDataMasker.CreateDefault())
    .WriteTo.Console()
    .CreateLogger();
```

扩展方法返回原 `LoggerConfiguration`，可以继续配置 sink 和其他 enricher。

## 脱敏边界

属性名包含 `password`、`secret`、`token` 或 `connectionstring` 时，标量值会按 `SensitiveDataKind.Token` 交给 `IDataMasker` 处理。属性名匹配不区分大小写。

## 注意事项

- `LoggerConfiguration` 或 `IDataMasker` 为 `null` 时抛出 `ArgumentNullException`
- 当前 enricher 只处理敏感标量属性；结构、序列和非敏感属性保持不变
- 调用方仍需避免把原始敏感载荷写入消息模板正文
- 本包不提供脱敏规则配置，规则与具体遮蔽算法由 `Tw.Security` 负责
