# Tw.Http

`Tw.Http` 提供出站 HTTP 请求头名称与可信传播策略。公开 API 位于 `Tw.Http` 和 `Tw.Http.HeaderPropagation` 命名空间。

## 能力边界

- `HttpHeaderNames` 提供平台约定的 Correlation 与租户请求头名称。
- `HeaderPropagationOptions` 保存调用方允许列表的不可变、不区分大小写快照。
- `HeaderPropagationPolicy` 只选择同时命中平台安全列表与调用方允许列表的现有请求头，不修改输入集合。
- `X-Tenant-Id` 只有在服务端完成租户验证并使用 `HeaderTrustLevel.Verified` 时才允许传播。
- `Authorization`、`Cookie`、`Set-Cookie` 和 `Proxy-Authorization` 始终拒绝自动传播，即使调用方把它们加入允许列表。

具体 `HttpMessageHandler` 与出站客户端注册属于 `Tw.Http`，不属于 provider-neutral 的 `Tw.Resilience`。当前包不提供没有真实注册行为与集成测试的占位注册入口。

## 选择出站请求头

```csharp
using Tw.Http.HeaderPropagation;

var inboundHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["traceparent"] = "00-trace-parent",
    ["X-Correlation-Id"] = "correlation-1",
    ["X-Tenant-Id"] = "tenant-1"
};

var options = new HeaderPropagationOptions(
    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "traceparent",
        "X-Correlation-Id",
        "X-Tenant-Id"
    });

var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
    inboundHeaders,
    options,
    HeaderTrustLevel.Verified);
```

调用方应当把 `selectedHeaders` 复制到新建的出站请求，不得修改或复用入站请求头集合。输入存在仅大小写不同的可传播同名请求头时，策略会拒绝产生歧义结果。

## 重试边界

出站 HTTP 自动重试只能用于读取操作，或具备明确幂等键、幂等窗口和重复请求响应语义的写操作。非幂等写操作不得自动重试。策略分类使用 `Tw.Resilience`，具体 handler 与客户端注册留在 `Tw.Http`。
