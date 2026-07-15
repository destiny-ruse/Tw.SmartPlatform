# Tw.Json.Newtonsoft

`Tw.Json.Newtonsoft` 使用 Newtonsoft.Json 实现 `Tw.Json.Abstractions`，公开 `NewtonsoftJsonSerializer`。

## 当前行为

- 属性名使用 camelCase
- 禁止引用环静默处理和类型名元数据
- `long` 与 `long?` 使用十进制字符串表示

## 稳定性

本包处于 `experimental` 阶段。进入 `stable` 前必须完成兼容读写、空值、枚举、日期、long、错误输入和持久 JSON 迁移验证；不得在同一 PackageId 下静默切换到其他 provider。
