# Tw.Domain.Shared

`Tw.Domain.Shared` 承载领域层可跨包共享的基础契约和轻量模型。它不包含业务规则执行、应用用例、MediatR handler、数据访问或权限检查。

## 使用方式

在领域包、应用契约包或服务契约中引用本包后，直接使用其中公开的共享类型。当前包不提供 DI 注册入口。

## 注意事项

- 共享类型必须保持领域无关或跨领域稳定复用
- 不得在本包引入 MediatR、FluentValidation、ORM、Web 或基础设施依赖
- 具体业务规则放入 `Tw.Domain` 或业务服务自身领域包
