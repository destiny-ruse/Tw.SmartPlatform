---
id: rules.comments-python
title: Python 注释规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, ai]
stacks: [python]
tags: [comments, python, code-style]
summary: 规定 Python docstring、类型说明和复杂逻辑注释方式。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# Python 注释规范

<!-- anchor: goal -->
## 目标

Python 注释规范用于降低动态语言中调用契约不清、类型意图隐藏、复杂数据处理难以维护的风险。Docstring 和注释必须补充类型标注无法表达的业务约束、异常和副作用。统一注释方式后，自动文档、测试和评审可以稳定理解模块边界。

<!-- anchor: scope -->
## 适用范围

本规范适用于 Python 模块、包、公共函数、类、方法、CLI 入口、任务脚本、测试夹具和示例代码中的 docstring、块注释和行内注释。它不适用于工具生成目录、第三方 vendored 源码、交互式 notebook 临时草稿或被生成器覆盖的客户端代码。

<!-- anchor: rules -->
## 规则

1. 公共模块、类、函数和 CLI 入口必须使用 docstring 说明职责、参数含义、返回值、异常或副作用；不得让调用方阅读实现猜测契约。
2. Docstring 必须与类型标注互补，说明业务约束、单位、时区、幂等性或外部依赖；不得重复类型标注已经清楚表达的信息。
3. 函数可能执行 I/O、网络调用、重试、事务或环境变量读取时，必须在 docstring 或相邻说明中写明边界。
4. 行内注释必须解释业务规则、兼容性、安全或性能取舍；不得逐行解释 Python 语法。
5. 模块级常量、魔法值或正则表达式若无法从名称理解原因，必须用注释说明来源和约束。
6. TODO、FIXME 注释必须说明触发条件和可追踪事项；不得留下无上下文的永久待办。
7. 测试注释应当说明特殊 fixture、冻结时间、mock 边界或回归场景；不得解释断言语法。
8. 生成代码目录不得添加人工维护注释；需要说明的内容必须写入生成源模板或相邻源定义。

<!-- anchor: examples -->
## 示例

正例：

```python
def cancel_order(order_id: str) -> None:
    """Cancel an order before settlement.

    Raises:
        ValueError: When the order is already settled or unknown.
    """
```

反例：

```python
# 遍历订单
for order in orders:
    # 添加订单
    result.append(order)
```

<!-- anchor: checklist -->
## 检查清单

- 公共模块、类、函数和 CLI 入口是否有必要 docstring。
- Docstring 是否补充业务约束、异常、副作用或外部依赖边界。
- 类型标注和 docstring 是否一致且互补。
- 行内注释是否解释原因和约束，而不是复述 Python 语法。
- TODO、FIXME 是否有可追踪上下文。
- generated 文件中是否避免人工维护注释。

<!-- anchor: relations -->
## 相关规范

- rules.comments-common
- rules.naming-python

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
