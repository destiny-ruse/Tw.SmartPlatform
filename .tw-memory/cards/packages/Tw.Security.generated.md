# Package: Tw.Security

标识：Tw.Security / backend/dotnet/BuildingBlocks/src/Foundation/Tw.Security / platform-team
职责：加密、哈希、密码哈希、密钥解析，以及敏感数据标记、脱敏、脱敏值识别和写回保护基础能力。

适用范围：
- 哈希、HMAC、PBKDF2 和密码学随机
- AES、DES、TripleDES、RSA 加解密与签名
- 密文布局和密钥格式解析
- 敏感数据类型声明
- 脱敏策略和规则契约
- 默认脱敏实现
- 脱敏值写回保护

不适用范围：
- 认证授权
- 密钥管理服务
- 审计日志落库

依赖边界：
- forbid: Microsoft.AspNetCore.*, SqlSugar*, DotNetCore.CAP*
- allow: Tw.Core

稳定性：experimental
兼容性：experimental 阶段不承诺兼容
迁移指针：

source_refs:
- charter:package-charter:Tw.Security
