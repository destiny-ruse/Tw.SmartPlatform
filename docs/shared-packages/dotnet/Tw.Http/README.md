# Tw.Http

`Tw.Http` 提供出站 HTTP 请求头名称与可信传播策略。公开 API 位于 `Tw.Http` 和 `Tw.Http.HeaderPropagation` 命名空间。

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须冻结可信请求头列表、租户传播条件、多值边界、出站客户端注册和重试失败语义。

## 能力边界

- `HttpHeaderNames` 提供平台约定的 Correlation 与租户请求头名称。
- `HeaderPropagationOptions` 保存调用方允许列表的不可变、不区分大小写快照。
- `HeaderPropagationPolicy` 只选择同时命中平台安全列表与调用方允许列表的现有请求头，不修改输入集合，并保留每个请求头的值顺序和值边界。
- `X-Tenant-Id` 只有在服务端完成租户验证并使用 `HeaderTrustLevel.Verified` 时才允许传播。
- `Authorization`、`Cookie`、`Set-Cookie` 和 `Proxy-Authorization` 始终拒绝自动传播，即使调用方把它们加入允许列表。

具体 `HttpMessageHandler` 与出站客户端注册属于 `Tw.Http`，不属于 provider-neutral 的 `Tw.Resilience`。当前包不提供没有真实注册行为与集成测试的占位注册入口。

## 选择出站请求头

```csharp
using Tw.Http.HeaderPropagation;

var inboundHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
{
    ["traceparent"] = ["00-trace-parent"],
    ["tracestate"] = ["vendor-a=value-a", "vendor-b=value-b"],
    ["X-Correlation-Id"] = ["correlation-1"],
    ["X-Tenant-Id"] = ["tenant-1"]
};

var options = new HeaderPropagationOptions(
    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "traceparent",
        "tracestate",
        "X-Correlation-Id",
        "X-Tenant-Id"
    });

var selectedHeaders = HeaderPropagationPolicy.SelectHeaders(
    inboundHeaders,
    options,
    HeaderTrustLevel.Verified);
```

`selectedHeaders` 是不区分名称大小写的不可变字典，每个值列表也都是独立的不可变快照；原始字典或值列表的后续修改不会影响结果。调用方应当把这些值逐项复制到新建的出站请求，不能用分隔符拼接多值。输入存在 null 值集合、null 值项或仅大小写不同的可传播同名请求头时，策略会拒绝产生结果；未定义的 `HeaderTrustLevel` 同样按失败关闭处理。

## 重试边界

出站 HTTP 自动重试只能用于读取操作，或具备明确幂等键、幂等窗口和重复请求响应语义的写操作。非幂等写操作不得自动重试。策略分类使用 `Tw.Resilience`，具体 handler 与客户端注册留在 `Tw.Http`。
