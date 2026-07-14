# Tw.Resilience

`Tw.Resilience` 提供公司自有的操作分类、韧性策略描述、输入校验和重试安全判断。该包不依赖 Polly、`Microsoft.Extensions.Http.Resilience` 或 `HttpClient` 类型。

## 能力边界

- `OperationKind` 区分读取、已具备幂等保护的写入和非幂等写入。
- `ResiliencePolicyDescriptor` 描述超时、重试次数、熔断、限流、并发隔离和降级意图。
- `ResiliencePolicyBuilder` 校验操作名称、超时和重试次数，并禁止非幂等写操作自动重试。

具体 HTTP handler、`HttpClient` 注册和第三方 provider 适配属于 `Tw.Http`。`Tw.Resilience` 不提供 HTTP 服务注册入口。

## 构建策略结果

```csharp
using Tw.Resilience;

var descriptor = ResiliencePolicyDescriptor.ForHttp(
    operationName: "GetOrder",
    operationKind: OperationKind.Read,
    timeout: TimeSpan.FromSeconds(3));

var policy = ResiliencePolicyBuilder.Build(descriptor);
```

具体适配器可以消费 `ResiliencePolicyDescriptor` 和 `ResiliencePolicy`，但不得把第三方 provider 类型反向暴露到该包的公开 API。

## 重试限制

读取操作可以在限定次数、退避策略和可重试错误类型后启用自动重试。写操作只有在明确声明幂等键、幂等窗口、重复请求响应和冲突处理后，才能使用 `OperationKind.IdempotentWrite`。未知写操作和 `OperationKind.NonIdempotentWrite` 不得自动重试。
