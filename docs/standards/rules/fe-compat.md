---
id: rules.fe-compat
title: 前端兼容性规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [frontend, ai]
stacks: [vue-ts]
tags: [frontend, vue-ts, code-style]
summary: 规定浏览器、设备、WebView 和降级策略。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 前端兼容性规范

<!-- anchor: goal -->
## 目标

前端兼容性规则用于降低浏览器、WebView、设备能力和网络环境差异导致的发布风险。变更必须明确依赖的运行环境和降级策略，使同一功能在支持范围内表现一致、在不支持范围内失败可控。

<!-- anchor: scope -->
## 适用范围

适用于 Web 页面、Vue 组件、构建产物、CSS 特性、浏览器 API、移动 WebView、设备能力调用、静态资源加载和兼容性验证说明。服务端逻辑和不进入用户运行环境的开发脚本不属于本规范范围；uni-app 专属多端条件编译按跨端规范处理。

<!-- anchor: rules -->
## 规则

1. 使用新的浏览器 API、CSS 特性或设备能力前必须确认目标浏览器、WebView 和设备支持范围。
2. 不稳定或可缺失的能力必须通过 feature detection、适配层或降级 UI 处理，不得只依赖 User-Agent 字符串判断。
3. 兼容性分支必须集中在 adapter、service 或组件封装内，不得在多个页面复制平台判断和魔法字符串。
4. 构建配置、polyfill、transpile 目标和静态资源格式变更必须说明对旧浏览器或 WebView 的影响。
5. 移动端布局必须验证窄屏、横竖屏、软键盘、刘海屏安全区和触摸目标尺寸，不得只以桌面视口验收。
6. 网络失败、超时、离线、接口字段缺失和资源加载失败必须有可感知降级，不得让页面永久空白或卡在 loading。
7. 兼容性修复必须附带验证说明，至少说明浏览器、WebView、设备类型、视口或模拟环境。
8. 不再支持某环境时必须在变更说明中写清边界和用户影响，不得静默移除降级路径。

<!-- anchor: examples -->
## 示例

正例：

```typescript
export function canShare(): boolean {
  return typeof navigator !== "undefined" && "share" in navigator;
}

if (canShare()) {
  await navigator.share({ title, url });
} else {
  copyLink(url);
  showToast(t("common.linkCopied"));
}
```

反例：

```typescript
if (navigator.userAgent.includes("iPhone")) {
  await navigator.share({ title, url });
}
```

<!-- anchor: checklist -->
## 检查清单

- 新 API、CSS 特性、资源格式或设备能力是否确认支持范围。
- 能力缺失时是否有检测、适配或降级 UI。
- 平台差异是否封装在单一边界，而不是散落在页面中。
- 移动端是否覆盖视口、软键盘、安全区和触摸验证。
- 网络、资源和字段异常是否不会导致永久空白或死 loading。
- 兼容性验证说明是否包含环境和结果。

<!-- anchor: relations -->
## 相关规范

- rules.fe-uniapp-cross
- rules.fe-typescript
- rules.test-strategy

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
