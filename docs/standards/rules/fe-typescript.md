---
id: rules.fe-typescript
title: 前端 TypeScript 规范
doc_type: rule
status: active
version: 1.0.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [vue-ts]
tags: [frontend, vue-ts, code-style]
summary: 规定类型声明、空值处理、异步调用和模块边界。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 前端 TypeScript 规范

<!-- anchor: goal -->
## 目标

让前端 TypeScript 规范在不同页面、团队和发布渠道中保持一致。

<!-- anchor: scope -->
## 适用范围

适用于前端页面、组件、路由、状态、构建配置和端侧能力适配。

<!-- anchor: rules -->
## 规则

1. 公共约定必须写入可复用模块或配置。
2. 页面逻辑、领域逻辑和平台适配必须分层。
3. 变更必须包含可验证的示例、测试或人工验收说明。

<!-- anchor: examples -->
## 示例

正例：把平台差异封装在适配层。

反例：在多个页面复制条件判断和魔法字符串。

<!-- anchor: checklist -->
## 检查清单

- 是否符合项目目录边界。
- 是否说明兼容性影响。
- 是否提供验证方式。

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
