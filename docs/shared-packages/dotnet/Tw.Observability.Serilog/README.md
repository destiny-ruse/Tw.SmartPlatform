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

属性名存在可靠边界时，会按非字母数字分隔符、大小写转换和缩写边界拆分为语义词，匹配不区分大小写。下列敏感语义可位于属性名的任意词位置：

- 单词：`password`、`secret`、`token`、`authorization`、`credential`、`cookie`
- 连续短语：`connection string`、`api key`、`private key`、`authorization header`、`cookie header`

当全大写或全小写属性名没有可靠单词边界时，enricher 会在移除受控 benign 后缀后扫描相同敏感语义的紧凑形式。因此 `ACCESSTOKEN`、`accesstoken`、`CLIENTSECRET`、`PASSWORDHASH` 和 `XAPIKEYVALUE` 也会脱敏。匹配到的标量值统一按 `SensitiveDataKind.Token` 交给 `IDataMasker` 处理。

为避免把框架概念、规则或实现元数据误判为敏感值，以下受控序列可作为 benign 后缀：`CancellationToken`、`TokenBucket`、`PasswordPolicy`、`AuthorizationPolicy`、`CredentialProvider`、`PrivateKeyAlgorithm`、`ConnectionStringBuilder`、`CookiePolicy`、`Secretariat`、`Tokenization` 和 `ApiKeyboard`。这些后缀可以直接结束，也可以追加一个受控元数据尾词：`Requested`、`Capacity`、`Name`、`Type`、`Count` 或 `Layout`。

例如 `CancellationTokenRequested`、`TokenBucketCapacity`、`PasswordPolicyName`、`AuthorizationPolicyName`、`CredentialProviderName`、`ConnectionStringBuilderType`、`SecretariatName`、`TokenizationCount` 和 `ApiKeyboardLayout` 不会被脱敏；对应的全大写或全小写紧凑形式遵循相同边界。

benign 元数据边界只忽略后缀本身，前缀仍会继续检查。例如 `TokenPasswordPolicy` 因前缀包含完整的 `token` 语义词，仍会被脱敏。

## 注意事项

- `LoggerConfiguration` 或 `IDataMasker` 为 `null` 时抛出 `ArgumentNullException`
- 当前 enricher 只处理敏感标量属性；结构、序列和非敏感属性保持不变
- 调用方仍需避免把原始敏感载荷写入消息模板正文
- 本包不提供属性名匹配规则配置，具体遮蔽算法由注入的 `IDataMasker` 负责
