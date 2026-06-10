# 配置与 Options 自动装载

## 能力定位

`Tw.DependencyInjection` 在 P3 阶段提供 Options 自动装载。业务配置类型只依赖 `Tw.Core` 中的 `Tw.Configuration.Abstractions` 契约与特性，组合根调用 `AddServiceRegistration(IConfiguration)` 后，引擎会在已纳入扫描的程序集内发现、绑定、校验 Options，并注册 `OptionsBindingReport` 诊断报告。

## 注册入口

```csharp
using Tw.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
builder.Services.AddServiceRegistration(builder.Configuration);
```

`AddServiceRegistration` 读取 `Tw:DependencyInjection` 扫描选项，复用程序集扫描结果。Options 自动装载不通过自身的 Options 子系统读取该配置节，避免自举循环。

## Options 类型

实现 `IConfigurableOptions` 的非抽象类会参与自动装载：

```csharp
using System.ComponentModel.DataAnnotations;
using Tw.Configuration.Abstractions;

public sealed class CacheOptions : IConfigurableOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
```

默认配置路径为类型名去掉 `Options` 后缀。上例绑定配置节 `Cache`：

```json
{
  "Cache": {
    "Endpoint": "localhost"
  }
}
```

## 显式路径与命名实例

使用 `[OptionsSection]` 指定稳定配置路径，使用 `[OptionsName]` 指定命名实例：

```csharp
[OptionsSection("Tw:Redis")]
[OptionsName("primary")]
public sealed class RedisOptions : IConfigurableOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}
```

```csharp
var redis = optionsMonitor.Get("primary");
```

未标记 `[OptionsName]` 的类型使用 `Options.DefaultName`。

## 后置配置

需要绑定后补默认值或派生非敏感字段时，实现 `IConfigurableOptions<TOptions>`：

```csharp
public sealed class CacheOptions : IConfigurableOptions<CacheOptions>
{
    public string Endpoint { get; set; } = string.Empty;
    public string EffectiveEndpoint { get; set; } = string.Empty;

    public void PostConfigure(CacheOptions options, IConfiguration configuration)
    {
        options.EffectiveEndpoint = configuration["Endpoint"] ?? options.Endpoint;
    }
}
```

`TOptions` 必须等于实现类自身类型。引擎把当前绑定的配置节传入 `PostConfigure`，不解析服务，也不使用 Service Locator。

## 校验

引擎对每个候选执行：

```csharp
services.AddOptions<TOptions>(name)
    .Bind(section)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

验证始终开启，不按环境门控。缺失配置节会在注册阶段抛出 `ServiceRegistrationException`，错误消息只包含配置路径和原因，不输出配置值。

类型实现 `IValidateOptions<TOptions>`，或使用 `[OptionsValidator(typeof(...))]` 指定校验器时，引擎会自动注册校验器。`Tw.Configuration.Abstractions.OptionsValidatorAttribute` 与 Microsoft Options 源生成器特性同名但命名空间不同，同一文件同时引用时使用命名空间限定或 using alias。

## 跳过与敏感配置

标记 `[DisableOptionsBinding]` 的类型跳过自动装载：

```csharp
[DisableOptionsBinding]
public sealed class LocalOnlyOptions : IConfigurableOptions
{
}
```

标记 `[SensitiveConfiguration]` 的类型或属性只影响诊断。`OptionsBindingReport` 输出类型、路径、命名实例、绑定状态、校验状态和敏感标记，不输出任何配置值。

## 注意事项

- Options 类型不作为普通服务注册，不参与 DI 单实现仲裁。
- 同一命名实例不得绑定到重复配置路径。
- 配置路径不得从环境名拼接，环境差异来自配置源。
- 密钥、令牌、证书和连接串必须来自受控密钥来源，不写入仓库配置样例。
