---
id: rules.dependency-policy
title: 依赖安全策略
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [architect, backend, frontend, qa, devops, ai]
stacks: [dotnet, java, python, vue-ts, uniapp]
tags: [security, compliance]
summary: 规定第三方依赖准入、漏洞处理和版本维护。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 依赖安全策略

<!-- anchor: goal -->
## 目标

依赖安全策略用于降低第三方组件带来的供应链、漏洞、许可证和维护风险。它要求依赖准入、版本固定、漏洞响应和例外记录具备可审查证据，让新增和升级依赖不会削弱系统安全与可维护性。

<!-- anchor: scope -->
## 适用范围

适用于后端、前端、移动端、构建脚本、测试工具、容器镜像和运行时引入的第三方库、插件、SDK、框架和二进制工具。仅用于本地一次性排查且不提交到仓库的工具不属于本规范范围；一旦进入锁文件、构建配置、镜像或 CI 环境，就必须遵守本规范。

<!-- anchor: rules -->
## 规则

1. 新增运行时依赖必须说明用途、替代方案、维护状态、许可证和安全影响，不得为少量胶水代码引入高风险依赖。
2. 依赖版本必须通过锁文件、版本目录或等价机制固定；不得在可复现构建中使用无上界浮动版本。
3. 依赖必须来自可信仓库或制品源，不得从个人网盘、未验证下载链接或不可追溯二进制引入生产依赖。
4. 新增或升级依赖必须通过漏洞扫描和许可证检查；发现高风险漏洞时必须升级、替换、移除或记录有到期时间的例外。
5. 直接依赖和关键传递依赖应当有明确所有者，过期、废弃或无人维护的依赖必须制定移除或替换计划。
6. 不得绕过安全扫描、删除锁文件或提交私有补丁而不记录来源、差异和维护责任。
7. 依赖升级必须评估破坏性变更，并为公共 API、序列化、认证、数据库驱动或构建链路补充验证。
8. 开发依赖不得泄漏到生产镜像或运行时包，除非其运行时职责被明确说明并通过评审。

<!-- anchor: examples -->
## 示例

正例：

```json
{
  "dependency": "org.example:json-parser",
  "version": "2.4.1",
  "reason": "替换无人维护的解析库",
  "license": "Apache-2.0",
  "securityReview": "scan passed, 2026-04-27"
}
```

正例说明：新增依赖有用途、固定版本、许可证和安全检查证据。

反例：

```json
{
  "dependencies": {
    "left-pad": "latest"
  }
}
```

反例说明：浮动版本不可复现，也缺少准入说明和安全证据。

<!-- anchor: checklist -->
## 检查清单

- 新增依赖是否说明用途、替代方案、维护状态、许可证和所有者。
- 版本是否被锁定，构建是否可复现。
- 依赖来源是否可信并能追溯到包管理器、制品库或供应商发布物。
- 漏洞和许可证扫描是否通过，例外是否有风险说明和到期时间。
- 升级或替换是否覆盖公共 API、认证、序列化、数据库驱动和构建链路风险。
- 开发依赖是否不会进入生产镜像或运行时包。

<!-- anchor: relations -->
## 相关规范

- processes.dependency-onboarding
- rules.secrets-management
- rules.ci-pipeline

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
