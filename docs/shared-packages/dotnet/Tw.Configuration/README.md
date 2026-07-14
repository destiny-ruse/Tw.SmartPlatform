# Tw.Configuration

`Tw.Configuration` 提供配置来源治理、敏感配置键契约、配置变更事件，以及 JSON 配置清单和安全路径校验。

## 注册方式

本包不注册运行时服务。调用方按需直接使用配置策略、清单工厂和路径校验器。

## 配置来源策略

```csharp
var allowed = ConfigurationSourcePolicy.IsUserSecretsAllowed("Development");
```

## JSON 配置路径校验

校验器先将输入转换为绝对路径，再拒绝通配符、允许根目录之外的路径和不存在的文件。调用方必须显式提供内容根目录和允许读取的配置根目录。

```csharp
var validator = new JsonConfigurationPathValidator(
    contentRoot: AppContext.BaseDirectory,
    allowedRoots: [Path.Combine(AppContext.BaseDirectory, "config")]);

var configurationPath = validator.Validate(
    Path.Combine(AppContext.BaseDirectory, "config", "appsettings.json"));
```

## JSON 配置清单

```csharp
var manifest = JsonConfigurationBuilderExtensions.CreateManifest(
    "appsettings.json",
    "appsettings.Production.json");
```

清单保留文件顺序，供调用方建立可预测的配置覆盖顺序。本包不读取密钥，也不提供 Nacos 配置源适配或服务发现。
