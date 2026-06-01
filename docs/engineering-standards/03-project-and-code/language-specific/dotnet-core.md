# .NET Core 专项规范

## 目标

统一 .NET Core 项目的解决方案组织、命名、XML 注释、依赖注入、配置、分层边界、异步编程和质量工具使用方式，保证服务、类库和工具项目具备一致工程质量。

## 适用范围

适用于 ASP.NET Core 服务、后台任务、类库、公共组件、SDK、命令行工具和 .NET Core 工具项目。

## 规范要求

- 项目必须记录 .NET SDK 版本、运行时版本、构建命令、测试命令和发布产物。
- 解决方案结构和项目引用必须保持清晰，不得出现循环引用。
- 命名空间和程序集必须使用 PascalCase 分段，并与目录、模块和领域边界一致。
- 类型、枚举、属性、事件和公共方法必须使用 PascalCase；局部变量和参数必须使用 camelCase；私有字段应当使用 `_camelCase`。
- 接口名称必须以 `I` 开头并表达能力或角色，不得使用 `IManager`、`IHelper` 等宽泛名称。
- 异步方法必须以 `Async` 结尾，并返回 `Task`、`Task<T>`、`ValueTask` 或等效异步类型；同步方法不得添加 `Async` 后缀。
- 公共类型、公共成员、接口方法和跨程序集可见 API 必须使用符合 DocFX 处理要求的 XML 文档注释说明用途和关键契约。
- 必须使用内置依赖注入或团队统一认可的依赖注入方式。
- `appsettings.*.json` 只能保存非密钥配置，密钥必须来自受控密钥来源。
- 项目必须启用 analyzers、格式化、静态检查和自动化测试。

## 分层与依赖

控制器必须保持协议适配边界，只负责参数绑定、基础校验、认证授权上下文读取和响应转换。业务规则必须放在 application service、domain service 或明确业务模块中。

项目依赖必须保持单向。Web 层可以依赖应用层，应用层可以依赖领域层或抽象接口，基础设施层实现数据库、缓存、消息队列、文件存储和第三方服务适配。

不得通过静态全局状态、Service Locator 或隐藏单例共享请求上下文。跨请求状态必须显式存储在受控持久化、缓存或上下文对象中。

## 异步、异常与配置

异步 I/O 必须使用 `async` / `await`。不得使用 `.Result`、`.Wait()` 或阻塞等待包装异步任务，避免线程池耗尽和死锁风险。

服务边界必须区分业务异常、验证异常、权限异常、依赖异常和未知异常。公共 API 的 XML 注释必须说明调用方可预期的异常类型或错误语义。

配置绑定类和配置节名称必须能从代码定位到配置路径。启动时必须校验必填配置、类型、范围和组合约束。

## DocFX XML 文档注释

.NET 公共 API 必须使用 XML 文档注释，并满足通用注释规则。标签使用要求如下：

| 元素 | 必需标签 | 按需标签 |
| --- | --- | --- |
| 类、接口、结构体 | `<summary>` | `<remarks>`、`<typeparam>`、`<seealso>` |
| 方法、构造函数 | `<summary>`、`<param>`、`<returns>` | `<exception>`、`<example>`、`<remarks>`、`<typeparam>` |
| 属性、字段、事件 | `<summary>` | `<value>`、`<remarks>` |
| 枚举和枚举值 | `<summary>` | `<remarks>` |

方法存在异常、取消、重试、幂等性、外部调用、性能成本或副作用时，必须通过 `<exception>`、`<remarks>` 或相邻说明表达。示例代码进入文档时，必须使用 `<example>` 和 `<code>` 包裹，并保持可编译或可理解。

```csharp
/// <summary>
/// 扫描指定程序集中的服务类型
/// </summary>
/// <param name="assembly">要扫描的程序集</param>
/// <param name="options">扫描配置选项</param>
/// <returns>发现的服务类型集合</returns>
/// <exception cref="ArgumentNullException">assembly 为 null 时抛出</exception>
public IEnumerable<Type> Scan(Assembly assembly, ServiceRegistrationOptions options)
```

## 常见反模式

- 控制器中堆积业务规则、数据库访问和第三方调用。
- 项目间循环引用，导致边界不可维护。
- 生产连接串写入 `appsettings.Production.json` 并提交仓库。
- 同步阻塞异步 I/O。
- 公共类型缺少 DocFX XML 文档注释，调用方只能阅读实现。
- 使用静态全局状态共享请求数据。

## 检查清单

- .NET SDK、运行时版本、构建命令和测试命令是否记录？
- 解决方案结构、项目命名和命名空间是否一致？
- 控制器是否保持薄层，业务规则是否离开基础设施细节？
- 异步方法命名、返回类型和调用方式是否符合要求？
- 公共 API 是否具备 DocFX XML 文档注释并说明异常语义？
- `appsettings.*.json` 是否只包含非密钥配置？
- analyzers、格式化、静态检查和测试是否可重复执行？
