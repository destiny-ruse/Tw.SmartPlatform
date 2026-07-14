# Tw.MultiTenancy

`Tw.MultiTenancy` 是提供方无关的当前租户契约包，包含租户身份值对象，以及认证后令牌租户与调用方提示租户的一致性解析。包内不读取 HTTP 上下文，也不访问租户存储。

## 公开能力

- `CurrentTenant`：当前租户身份值对象，`Default` 表示未解析到显式租户
- `ICurrentTenant`：由宿主实现的当前租户读取契约
- `TenantResolver`：比较令牌租户与提示租户并生成 `CurrentTenant`
- `TenantMismatchException`：两个非空租户标识不一致时的失败类型

## 使用方式

该包不提供 DI 注册入口。调用方可以直接创建无状态解析器，并由宿主按自身调用边界实现 `ICurrentTenant`：

```csharp
var resolver = new TenantResolver();
var tenant = resolver.Resolve(
    tokenTenantId: "tenant-a",
    hintedTenantId: "tenant-a");
```

仅提供一个租户标识时，解析器返回该租户；两个标识均缺失时返回 `CurrentTenant.Default`；两个非空标识不一致时抛出 `TenantMismatchException`。

## 能力边界

- 包保持提供方无关，不依赖 ASP.NET Core、SqlSugar、CAP 或其他租户提供方
- JWT 签名与声明校验由认证边界负责
- HTTP 请求头、路由参数或查询参数访问器由 Web 适配层负责
- 租户目录、租户配置和持久化存储由具体提供方或业务服务负责
- 包不校验租户标识格式，也不提供分片策略
