# uni-app 跨端规范

## 目标

uni-app 跨端代码必须显式管理平台能力、条件编译和运行差异，避免某一端修复破坏另一端。统一跨端规则可以让页面、组件和服务在 H5、小程序、App 等目标端之间保持可验证的行为边界。

## 适用范围

适用于 uni-app 项目的页面、组件、组合式函数、`pages.json`、平台条件编译、端侧 API 调用、静态资源、样式适配、构建配置和多端验收说明。纯 Vue Web 项目和后端服务不属于本规范范围；共享 TypeScript 逻辑被 uni-app 消费时必须符合跨端能力边界。

## 规则

1. 平台差异必须优先封装在 adapter、service 或 composable 中，不得在多个页面复制 `#ifdef` 和平台字符串。
2. 条件编译块必须保持最小范围，只包住确实存在平台差异的代码，不得包裹整段无关业务逻辑。
3. 使用相机、定位、分享、支付、文件、蓝牙等端侧能力前必须声明支持平台、权限前置条件和失败降级。
4. H5、小程序和 App 的生命周期差异必须在页面或 composable 边界处理，不得假设所有端都有同一事件顺序。
5. 跨端样式必须使用 uni-app 支持的单位、选择器和安全区处理方式，不得依赖只在浏览器中稳定的 CSS 特性。
6. 静态资源必须考虑各端包体、路径、缓存和格式限制，大资源或端专属资源应当分平台放置或懒加载。
7. `pages.json`、路由路径、页面标题和导航样式变更必须同时说明影响的平台。
8. 每个跨端能力变更必须附带验证说明，至少覆盖受影响目标端或说明未覆盖端的风险。

## 示例

正例：

```typescript
export async function chooseAvatar(): Promise<string | null> {
  // #ifdef MP-WEIXIN || APP-PLUS
  const result = await uni.chooseImage({ count: 1 });
  return result.tempFilePaths[0] ?? null;
  // #endif

  // #ifdef H5
  showToast(t("profile.avatar.unsupportedOnCurrentPlatform"));
  return null;
  // #endif
}
```

反例：

```typescript
// 多个页面复制同一平台判断，且没有 H5 降级。
// #ifdef MP-WEIXIN
uni.chooseImage({ count: 1 });
// #endif
```

## 检查清单

- 平台差异是否封装在 adapter、service 或 composable 中。
- 条件编译范围是否最小且只覆盖真实差异。
- 端侧能力是否说明权限、失败和降级策略。
- 生命周期、样式、资源和路径是否考虑 H5、小程序、App 差异。
- `pages.json` 或导航变更是否说明目标端影响。
- 验证说明是否覆盖受影响平台。

## 相关规范

- rules.naming-uniapp
- rules.fe-compat
- rules.fe-typescript

## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
