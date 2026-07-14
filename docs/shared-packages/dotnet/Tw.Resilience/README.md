# Tw.Resilience

`Tw.Resilience` 提供公司自有的操作分类、韧性策略描述、输入校验和重试安全判断。该生产项目保持零依赖：项目文件不声明包、项目、框架或普通程序集引用，锁文件中每个目标框架的依赖图均为空。

## 能力边界

- `OperationKind` 区分读取、已具备幂等保护的写入和非幂等写入。
- `ResiliencePolicyDescriptor` 描述超时、重试次数、熔断、限流、并发隔离和降级意图。
- `ResiliencePolicyBuilder` 校验操作名称、操作分类、超时和重试次数，并把非幂等写操作的有效重试次数归一化为零。
- `ResiliencePolicy` 完整保留已验证的操作名称、操作分类、超时、归一化重试次数、熔断、速率限制、并发隔离与降级意图；`RetryEnabled` 只由归一化后的 `RetryCount` 派生。

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

具体适配器只能消费 `ResiliencePolicyBuilder.Build` 返回的完整已验证 `ResiliencePolicy`，不得直接把未经验证的 `ResiliencePolicyDescriptor` 映射为 provider 配置，也不得把第三方 provider 类型反向暴露到该包的公开 API。适配器必须从该结果读取操作名称、操作分类、超时、有效重试次数以及四项开关意图，不得回读原始描述补齐字段。

provider-neutral 层不虚构通用上限：它接受所有大于零的 `TimeSpan` 超时和所有非负 `int` 重试次数。每个具体 provider 适配器必须在映射前额外校验自身支持的超时和重试次数上限。

## 重试限制

读取操作可以在限定次数、退避策略和可重试错误类型后启用自动重试。写操作只有在明确声明幂等键、幂等窗口、重复请求响应和冲突处理后，才能使用 `OperationKind.IdempotentWrite`。未知操作分类会被拒绝；`OperationKind.NonIdempotentWrite` 的声明和有效重试次数均归一化为零。
