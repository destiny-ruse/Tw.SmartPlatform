# Python 命名规范

## 目标

Python 命名规范用于降低模块职责不清、动态对象难以理解、常量与配置混淆和外部字段扩散的风险。命名必须符合 Python 生态约定，并通过名称表达领域含义、类型意图和副作用边界。统一命名后，测试、类型检查、文档和脚本入口可以保持可读。

## 适用范围

本规范适用于 Python 包、模块、类、函数、方法、变量、参数、常量、异常、测试、CLI 入口和配置对象命名。它不适用于工具生成目录、第三方 vendored 源码、必须保持的外部 JSON 字段名或 notebook 中不进入仓库历史的临时探索代码。

## 规则

1. 包、模块、函数、方法、变量和参数必须使用 snake_case；不得使用 camelCase 或含义不明的 `data`、`obj`、`tmp` 作为长期名称。
2. 类和异常必须使用 PascalCase，异常类应当以 `Error` 结尾或使用项目内稳定异常命名；不得使用 `Base`、`Common` 作为主要语义。
3. 常量必须使用 UPPER_SNAKE_CASE，并只表示真正稳定的不可变值；不得把环境配置或运行时状态命名为常量。
4. 布尔变量和函数应当使用 `is_`、`has_`、`can_`、`should_` 等判断语义；不得使用 `flag` 表达长期业务状态。
5. 私有模块成员应当使用单下划线前缀；不得用双下划线制造不必要的名称改写。
6. 异步函数必须使用 `async def`，名称应当表达 I/O、任务或等待语义；不得用 `_async` 后缀伪装同步函数。
7. 测试函数必须使用 `test_<scenario>_<expected>` 或项目约定的可读结构；不得使用 `test1`、`test_ok`。
8. 外部协议字段和生成代码名称必须隔离在边界层；进入领域模型后应当转换为 Python 命名风格。

| 构件 | 命名方式 | 示例 |
| --- | --- | --- |
| 模块/包 | snake_case | `order_service` |
| 类/异常 | PascalCase | `OrderService`, `OrderStateError` |
| 函数/变量 | snake_case | `calculate_total` |
| 布尔值 | is_/has_/can_/should_ + snake_case | `can_cancel` |
| 常量 | UPPER_SNAKE_CASE | `MAX_RETRY_COUNT` |

## 示例

正例：

```python
MAX_RETRY_COUNT = 3

async def fetch_order_status(order_id: str) -> OrderStatus:
    ...

def can_cancel_order(order: Order) -> bool:
    return order.status == OrderStatus.PENDING
```

反例：

```python
data = {}
flag = True
def GetOrderAsync(id):
    ...
```

## 检查清单

- 包、模块、函数、变量和参数是否使用 snake_case。
- 类、异常和常量是否符合 Python 命名形态。
- 布尔值、异步函数和测试函数是否从名称可判断语义。
- 是否避免万能名称、编号、无意义缩写和伪常量。
- 外部协议字段和 generated 名称是否被隔离或转换。
- 命名是否与类型标注、docstring 和测试场景一致。

## 相关规范

- rules.naming-common
- rules.comments-python

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
