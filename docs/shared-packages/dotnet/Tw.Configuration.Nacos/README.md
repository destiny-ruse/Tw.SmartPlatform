# Tw.Configuration.Nacos

`Tw.Configuration.Nacos` 将 Nacos 配置源接入 `Tw.Configuration` 的配置变更契约。

## 稳定性

本包处于 `experimental` 阶段。在完成真实 Nacos provider 集成测试前，不承诺兼容性或生产稳定性。

## 注册方式

宿主通过 `nacos-sdk-csharp.Extensions.Configuration` 注册 Nacos 配置源，再使用本包桥接经过校验的配置变更。本包不提供额外的依赖注入注册入口。

## 配置变更桥接

```csharp
var bridge = new NacosConfigurationBridge();
var change = bridge.AcceptChange("Features:Checkout", "Nacos");
```

## 边界

- 仅负责 Nacos 配置源适配和配置变更事件
- 不负责密钥存储或 JSON 路径扫描
- 服务发现不在本包范围内，也不依赖 `nacos-sdk-csharp.Extensions.ServiceDiscovery`
