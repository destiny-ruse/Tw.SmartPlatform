# Tw.Domain

`Tw.Domain` 承载领域实体、值对象、领域服务和领域异常等应用无关的业务规则基础。它可以依赖 `Tw.Domain.Shared` 和 `Tw.Core`，但不承载应用用例编排。

## 使用方式

服务的领域层项目引用本包后，按领域对象或领域服务组织业务规则。当前包不提供 DI 注册入口。

## 注意事项

- 不得在本包定义 MediatR handler 或应用 pipeline
- 不得依赖 SqlSugar、CAP、OpenIddict、ASP.NET Core 等基础设施或协议包
- 跨服务共享前先确认规则确实属于平台领域基础，而不是单个服务私有模型
