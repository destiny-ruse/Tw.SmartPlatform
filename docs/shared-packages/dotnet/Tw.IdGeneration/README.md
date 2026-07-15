# Tw.IdGeneration

`Tw.IdGeneration` 定义公司拥有的长整型标识生成消费端口 `IIdGenerator`。

## 稳定性与边界

本包处于 `experimental` 阶段。节点分配、ID 位布局、时钟回拨和具体算法属于 provider；稳定前必须冻结消费端口的并发、取消和失败语义。

Yitter 实现在 [`Tw.IdGeneration.Yitter`](../Tw.IdGeneration.Yitter/README.md) 中，消费方不得直接依赖 Yitter SDK 类型。
