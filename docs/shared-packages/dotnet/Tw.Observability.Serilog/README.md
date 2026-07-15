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

属性名会按非字母数字分隔符、大小写转换和缩写边界拆分为语义词，匹配不区分大小写。下列敏感语义可位于属性名的任意词位置：

- 单词：`password`、`secret`、`token`、`authorization`、`credential`、`cookie`
- 连续短语：`connection string`、`api key`、`private key`、`authorization header`、`cookie header`

例如 `PasswordHash`、`ClientSecretValue`、`TokenPayload`、`ConnectionStringValue`、`ApiKeyValue` 和 `PrivateKeyPem` 的标量值都会按 `SensitiveDataKind.Token` 交给 `IDataMasker` 处理。

为避免把规则或实现元数据误判为敏感值，以下语义后缀属于明确的 benign 元数据边界：`PasswordPolicy`、`AuthorizationPolicy`、`CredentialProvider`、`PrivateKeyAlgorithm`、`ConnectionStringBuilder` 和 `CookiePolicy`。普通词中的字母片段也不会被当作完整敏感词，例如 `SecretariatName`、`TokenizationCount` 和 `ApiKeyboardLayout` 不会被脱敏。

benign 元数据边界只忽略后缀本身，前缀仍会继续检查。例如 `TokenPasswordPolicy` 因前缀包含完整的 `token` 语义词，仍会被脱敏。

## 注意事项

- `LoggerConfiguration` 或 `IDataMasker` 为 `null` 时抛出 `ArgumentNullException`
- 当前 enricher 只处理敏感标量属性；结构、序列和非敏感属性保持不变
- 调用方仍需避免把原始敏感载荷写入消息模板正文
- 本包不提供属性名匹配规则配置，具体遮蔽算法由注入的 `IDataMasker` 负责
