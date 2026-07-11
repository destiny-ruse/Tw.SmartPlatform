# Tw.Security

`Tw.Security` 提供密码学与敏感数据保护能力。本包处于 experimental 阶段，密码学静态 API 位于 `Tw.Security.Cryptography`，敏感数据脱敏 API 位于 `Tw.Security.DataMasking`。

## 能力索引

- [密码学使用指南](cryptography.md)：哈希、HMAC、PBKDF2、密码学安全随机、对称加密和 RSA 的入口与约束。

## 依赖边界

| 依赖项 | 说明 |
| --- | --- |
| `Tw.Core` | `Check` 参数守卫与基础异常 |

不依赖：ASP.NET Core、数据库驱动、认证授权实现、密钥管理服务或审计日志持久化。

## 使用边界

本包不提供 DI 注册入口。密码学能力通过静态方法直接调用；调用方负责从受控密钥来源获取密钥，并负责按业务场景选择算法和密文存储策略。
