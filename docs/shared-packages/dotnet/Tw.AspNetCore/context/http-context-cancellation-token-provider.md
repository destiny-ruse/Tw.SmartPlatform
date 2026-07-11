# HttpContext 取消令牌 Provider 迁移说明

`HttpContextCancellationTokenProvider` 当前由 `Tw.AspNetCore.Mvc` 承载。`Tw.AspNetCore` host-level 启动入口不注册 HTTP 请求取消令牌 provider。

当前文档入口：

- [`Tw.AspNetCore.Mvc` HttpContext 取消令牌 Provider](../../Tw.AspNetCore.Mvc/context/http-context-cancellation-token-provider.md)

在 MVC/Web API 应用中，引用 `Tw.AspNetCore.Mvc` 后调用：

```csharp
using Tw.AspNetCore.Mvc.Context;

builder.Services.AddHttpContextCancellationTokenProvider();
```

需要启用 MVC 集成能力时，调用：

```csharp
using Tw.AspNetCore.Mvc;

builder.Services.AddMvcIntegration();
```
