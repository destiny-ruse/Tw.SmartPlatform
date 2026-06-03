# Cancellation Token Provider 设计

## 背景

本设计为后端 .NET 共享包增加统一的取消令牌上下文能力。实现参考 ABP Framework 的 Cancellation Token Provider 思路，但按当前工程边界落在 `Tw.Core`，作为系统核心能力提供给 HTTP API、gRPC、DotNetCore.CAP 消费、HostedService、Worker、后台任务和定时任务等微服务入口使用。

当前仓库的 .NET BuildingBlocks 结构为：

- `backend/dotnet/BuildingBlocks/src/Tw.Core`：跨服务复用的基础原语与无框架依赖工具
- `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore`：ASP.NET Core 宿主集成能力

取消令牌上下文属于跨入口、跨服务的核心执行上下文能力，不拆分为独立 `Threading` 包。

## 目标

- 在 `Tw.Core` 中提供框架无关的 `ICancellationTokenProvider`
- 支持入口层将当前执行 token 写入异步调用链上下文
- 支持 HTTP API 自动使用 `HttpContext.RequestAborted`
- 支持 gRPC、CAP 消费、HostedService、Worker、后台任务等入口通过 `Use(token)` 统一建立上下文
- 支持业务服务在显式 token 缺省时回退到当前入口 token
- 为共享包能力建立内部使用文档目录和强制文档更新规则

## 非目标

- 不新增独立 `Tw.Threading` 项目或 NuGet 包
- 不在本次能力中直接引用 DotNetCore.CAP、Hangfire、Quartz 或其他调度框架
- 不实现重试、超时、限流、补偿或消息幂等逻辑
- 不改变现有 API 响应结构、错误响应契约或网关规则

## 方案选择

采用“核心抽象 + 入口作用域”的方案：

- `Tw.Core` 提供通用 provider、AsyncLocal 作用域、默认 provider 和扩展方法
- `Tw.AspNetCore` 提供 ASP.NET Core 适配器，优先使用上下文覆盖 token，否则读取 `HttpContext.RequestAborted`
- CAP、后台任务、HostedService、Worker、gRPC 显式上下文场景由入口代码调用 `Use(token)` 建立作用域

该方案减少公共包依赖，避免将 CAP、Quartz、Hangfire 等框架耦合进核心包，同时覆盖 B/S 前后端分离应用中常见的微服务入口。

## 架构

### `Tw.Core`

新增目录：

```text
backend/dotnet/BuildingBlocks/src/Tw.Core/Context/
```

命名空间：

```csharp
Tw.Context
```

公开组件：

- `ICancellationTokenProvider`
- `CancellationTokenProviderBase`
- `CancellationTokenOverride`
- `AsyncLocalCancellationTokenScopeProvider`
- `NullCancellationTokenProvider`
- `CancellationTokenProviderExtensions`
- `ServiceCollection` 注册扩展

`Tw.Core` 不引用 `Microsoft.AspNetCore.*`，保持无 Web 框架依赖。

### `Tw.AspNetCore`

新增目录：

```text
backend/dotnet/BuildingBlocks/src/Tw.AspNetCore/Context/
```

命名空间：

```csharp
Tw.AspNetCore.Context
```

公开组件：

- `HttpContextCancellationTokenProvider`
- ASP.NET Core DI 注册扩展

`Tw.AspNetCore` 依赖 `Tw.Core`，并通过注册扩展将 `ICancellationTokenProvider` 替换为 HTTP provider。

## 组件契约

### `ICancellationTokenProvider`

职责：

- 提供当前执行上下文的 `CancellationToken`
- 允许入口层临时覆盖当前执行上下文 token

接口形态：

```csharp
public interface ICancellationTokenProvider
{
    CancellationToken Token { get; }

    IDisposable Use(CancellationToken cancellationToken);
}
```

语义：

- `Token` 返回当前作用域 token
- 没有作用域时由具体 provider 决定默认值
- `Use` 返回的 `IDisposable` 释放后恢复外层作用域
- 支持嵌套作用域

### `CancellationTokenProviderBase`

职责：

- 封装 override 读取与 `Use(token)` 作用域创建
- 避免每个 provider 重复实现 AsyncLocal 作用域逻辑

语义：

- 派生类只实现自身默认 token 来源
- override token 的优先级高于派生类默认 token

### `AsyncLocalCancellationTokenScopeProvider`

职责：

- 使用 `AsyncLocal` 维护异步调用链内的作用域栈
- 支持 async/await 后仍能读取入口 token
- 支持嵌套作用域恢复

约束：

- 不使用静态全局可变业务状态
- 不跨独立异步执行链传播 token
- 作用域释放必须清理当前节点并恢复外层节点

### `NullCancellationTokenProvider`

职责：

- 为非 Web 入口和未显式设置 token 的场景提供稳定默认值

语义：

- 没有 override 时返回 `CancellationToken.None`
- 有 override 时返回 override token

### `CancellationTokenProviderExtensions`

职责：

- 为业务服务提供显式 token 优先的统一写法

方法：

```csharp
public static CancellationToken FallbackToProvider(
    this ICancellationTokenProvider provider,
    CancellationToken preferredValue = default)
```

语义：

- `preferredValue` 不是 `default` 且不是 `CancellationToken.None` 时返回显式 token
- 显式 token 缺省时返回 `provider.Token`

### `HttpContextCancellationTokenProvider`

职责：

- 在 ASP.NET Core 宿主中提供 HTTP 请求取消 token

语义：

- override token 优先
- 有 `HttpContext` 时返回 `HttpContext.RequestAborted`
- 无 `HttpContext` 时返回 `CancellationToken.None`

## 入口使用方式

### HTTP API

ASP.NET Core 应用注册 `Tw.AspNetCore` 后，业务服务注入 `ICancellationTokenProvider` 即可读取当前请求的 `RequestAborted`。

业务服务方法存在显式 `CancellationToken` 参数时，使用 `FallbackToProvider`：

```csharp
public Task HandleAsync(CancellationToken cancellationToken = default)
{
    var effectiveToken = cancellationTokenProvider.FallbackToProvider(cancellationToken);
    return repository.SaveAsync(effectiveToken);
}
```

### gRPC

gRPC 服务运行在 ASP.NET Core 宿主中时，默认 provider 可读取 HTTP request aborted。服务方法显式获取 `ServerCallContext.CancellationToken` 时，在方法入口建立作用域：

```csharp
using (cancellationTokenProvider.Use(context.CancellationToken))
{
    return await applicationService.HandleAsync();
}
```

### DotNetCore.CAP 消费

本能力不直接依赖 CAP 包。CAP consumer 在入口方法中从可用上下文取得取消 token，然后建立作用域：

```csharp
using (cancellationTokenProvider.Use(cancellationToken))
{
    await applicationService.HandleAsync();
}
```

CAP 消费的重试、死信、幂等和确认边界由消息消费设计负责，不由 cancellation provider 承担。

### HostedService / Worker

`BackgroundService.ExecuteAsync` 使用 `stoppingToken` 建立作用域：

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using (cancellationTokenProvider.Use(stoppingToken))
    {
        await workerLoop.RunAsync();
    }
}
```

### 后台任务和定时任务

调度框架提供 token 时，入口使用 `Use(token)` 建立当前执行上下文。调度框架没有提供 token 时，provider 返回 `CancellationToken.None`，业务服务仍能通过统一接口运行。

## 数据流

HTTP API 数据流：

```text
HTTP request
  -> HttpContext.RequestAborted
  -> HttpContextCancellationTokenProvider.Token
  -> application service
  -> repository / external client
```

显式入口 token 数据流：

```text
entry token
  -> ICancellationTokenProvider.Use(token)
  -> AsyncLocalCancellationTokenScopeProvider
  -> ICancellationTokenProvider.Token
  -> application service
  -> downstream operation
```

业务服务显式参数数据流：

```text
method cancellationToken
  -> FallbackToProvider(cancellationToken)
  -> explicit token or provider.Token
  -> downstream operation
```

## 错误处理与取消语义

- `OperationCanceledException` 表达正常取消信号，不在业务层吞掉后返回成功
- 边界层记录取消时应区分客户端断开、宿主停止和业务超时
- Provider 不生成错误响应、不改变状态码、不负责异常映射
- Provider 不主动取消 token，只传播入口提供的取消信号
- 没有入口 token 时稳定返回 `CancellationToken.None`

## 依赖注入

`Tw.Core` 注册：

- `AsyncLocalCancellationTokenScopeProvider` 使用 singleton
- `ICancellationTokenProvider` 默认注册为 `NullCancellationTokenProvider`

`Tw.AspNetCore` 注册：

- `IHttpContextAccessor`
- 将 `ICancellationTokenProvider` 替换为 `HttpContextCancellationTokenProvider`

注册扩展命名应表达能力，例如：

- `AddTwCore`
- `AddTwAspNetCore`

## 测试设计

### `Tw.Core.Tests`

覆盖场景：

- 默认 provider 无 override 时返回 `CancellationToken.None`
- `Use(token)` 后 provider 返回指定 token
- `Use(token)` 释放后恢复 `CancellationToken.None`
- 嵌套 `Use(token)` 释放内层后恢复外层 token
- async/await 后仍能读取 override token
- `FallbackToProvider` 在显式 token 存在时优先返回显式 token
- `FallbackToProvider` 在显式 token 缺省时返回 provider token

### `Tw.AspNetCore.Tests`

覆盖场景：

- 有 `HttpContext` 时返回 `RequestAborted`
- override token 优先级高于 `RequestAborted`
- 无 `HttpContext` 且无 override 时返回 `CancellationToken.None`

## 文档治理

新增共享包使用文档目录：

```text
docs/shared-packages/
|-- README.md
`-- dotnet/
    |-- README.md
    `-- Tw.Core/
        |-- README.md
        `-- context/
            `-- cancellation-token-provider.md
```

目录职责：

- `docs/shared-packages/README.md`：共享包文档总索引，按语言跳转
- `docs/shared-packages/dotnet/README.md`：.NET 共享包索引，按包跳转
- `docs/shared-packages/dotnet/Tw.Core/README.md`：`Tw.Core` 能力索引，按功能跳转
- `docs/shared-packages/dotnet/Tw.Core/context/cancellation-token-provider.md`：取消令牌 provider 使用文档

文档类型：

- 索引页采用 Reference 文档
- 功能使用文档采用 How-to Guide 文档
- 复杂机制的补充说明采用 Explanation 文档

本功能使用文档覆盖：

- 能力定位
- DI 注册方式
- HTTP API 使用方式
- gRPC 使用方式
- DotNetCore.CAP 消费入口使用方式
- HostedService / Worker / 后台任务使用方式
- 业务服务中 `FallbackToProvider` 的推荐写法
- 注意事项

## 工程规则更新

正式规则更新：

- 修改 `docs/engineering-standards/03-project-and-code/shared-package-charter.md`
- 新增共享包能力文档要求
- 规则内容：新增或修改共享包公开能力时，必须同步创建或更新 `docs/shared-packages/<language>/<package>/<feature>.md` 以及相关索引

AI 加载索引更新：

- 更新 `.rules/ai-coding-rules` 中的任务路由
- 共享包开发、共享包能力变更、公共构建块变更必须加载共享包 charter 正式规范
- `.rules` 只作为加载索引，不复制正式规则正文

## 实施范围

本设计进入实施计划后包含以下工作：

- `Tw.Core` cancellation provider 核心实现
- `Tw.AspNetCore` HTTP provider 适配
- 必要项目引用和 DI 注册扩展
- `Tw.Core.Tests` 与 `Tw.AspNetCore.Tests`
- 共享包文档目录和本功能使用文档
- 工程规范正文更新
- `.rules` 加载索引更新

## 验收标准

- `Tw.Core` 不引用 `Microsoft.AspNetCore.*`
- `Tw.AspNetCore` 可替换 `ICancellationTokenProvider` 为 HTTP provider
- HTTP、gRPC、CAP、HostedService、Worker、后台任务入口均有明确使用方式
- provider 支持嵌套作用域和 async/await 上下文传播
- 所有新增公共 API 具备 XML 文档注释
- 单元测试覆盖默认 token、override、嵌套、异步传播和 fallback 语义
- `docs/shared-packages` 具备可跳转索引和取消令牌 provider 使用文档
- 共享包文档强制要求写入正式工程规范，并由 `.rules` 路由加载
