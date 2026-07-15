# Tw.Templates

`Tw.Templates` 提供 Tw .NET 框架官方 `dotnet new` 模板。

## 稳定性

本工具处于 `experimental` 阶段。模板短名称、参数、输出目录、包引用和锁定恢复行为在稳定前必须通过模板实例化门禁。

## 安装

```powershell
dotnet pack backend/dotnet/tools/src/Tw.Templates/Tw.Templates.csproj -o artifacts/templates
dotnet new install (Get-ChildItem artifacts/templates/Tw.Templates*.nupkg | Select-Object -First 1).FullName --force
```

## 模板

- `tw-service`：包含 domain、application、HTTP API 和 host 项目的服务解决方案
- `tw-gateway`：网关宿主骨架
- `tw-building-block`：包含 package charter 与测试的共享包骨架
- `tw-contract-package`：包含 HTTP DTO、CAP event、proto 和错误码的契约包骨架

模板不得生成淘汰包名称；模板示例源码作为 NuGet Content 打包，不属于 `Tw.Templates` 自身的公开 API。
