---
id: rules.fe-i18n
title: 前端国际化规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [vue-ts]
tags: [frontend, vue-ts, code-style]
summary: 规定文案键、语言包、占位符和回退策略。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 前端国际化规范

<!-- anchor: goal -->
## 目标

前端国际化规则用于避免硬编码文案、占位符错配和多语言界面不可访问的问题。统一语言键、参数格式和回退策略可以让页面文案在翻译、测试和运行期切换时保持稳定。

<!-- anchor: scope -->
## 适用范围

适用于 Vue 页面、组件、表单、路由标题、通知、错误提示、空状态、语言包 JSON 或 TypeScript 配置、测试 fixture 和文案评审。协议字段、日志字段、外部系统固定枚举和不展示给用户的内部标识不属于本规范范围；一旦内容展示给用户，必须纳入国际化处理。

<!-- anchor: rules -->
## 规则

1. 用户可见文案必须通过语言键引用，不得在模板、组件逻辑、校验规则或通知调用中硬编码。
2. 语言键必须稳定、可搜索，并按领域或页面组织，不得使用翻译内容本身作为 key。
3. 占位符必须使用具名参数，并在所有语言包中保持同名、同语义和同格式。
4. 语言包不得拼接半句生成完整语句；涉及数量、日期、货币和性别等变化时应当使用支持复数或格式化的工具函数。
5. 缺失翻译必须有明确回退策略，并在测试或检查中暴露，不得静默显示 key 或空字符串给用户。
6. 表单 label、placeholder、错误提示和辅助说明必须分别配置，不得用 placeholder 替代 label。
7. 路由标题、弹窗标题、ARIA 名称和图标按钮说明也必须使用国际化文案。
8. 删除页面或组件时应当同步清理不再使用的语言键，避免语言包长期膨胀。

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "order": {
    "receiver": {
      "label": "收货人",
      "placeholder": "请输入收货人"
    }
  }
}
```

```vue
<label for="receiver">{{ t("order.receiver.label") }}</label>
<input id="receiver" :placeholder="t('order.receiver.placeholder')" />
```

反例：

```vue
<button>提交订单</button>
<input placeholder="请输入收货人" />
{{ t("共") + count + t("件商品") }}
```

<!-- anchor: checklist -->
## 检查清单

- 用户可见文案是否全部通过语言键引用。
- 语言键是否按领域组织、稳定且不直接使用翻译文本。
- 占位符是否为具名参数，并在所有语言中一致。
- label、placeholder、错误提示和 ARIA 名称是否分别处理。
- 缺失翻译、日期、数量和货币格式是否有可验证回退。
- 删除功能时是否同步清理无用语言键。

<!-- anchor: relations -->
## 相关规范

- rules.fe-a11y
- rules.fe-typescript
- rules.fe-vue-ts-project

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
