---
id: rules.k8s-resource-naming
title: Kubernetes 资源命名规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, frontend, qa, devops, ai]
stacks: []
tags: [cicd, deployment, operations]
summary: 规定命名空间、工作负载、服务和配置资源命名。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# Kubernetes 资源命名规范

<!-- anchor: goal -->
## 目标

Kubernetes 资源命名规范用于降低环境混淆、资源归属不明、服务发现错误和生成清单覆盖人工配置的风险。资源名称必须能表达系统、组件、环境和用途，让部署、监控、回滚和排障时可以稳定定位对象。命名还应当避免与 Kubernetes 限制和工具生成规则冲突。

<!-- anchor: scope -->
## 适用范围

本规范适用于 Kubernetes namespace、Deployment、StatefulSet、Job、CronJob、Service、Ingress、ConfigMap、Secret、ServiceAccount、HPA、PVC、Helm values、Kustomize overlay 和生成的部署清单。它不适用于集群供应商预置资源、第三方 chart 内部资源或临时本地 kind/minikube 实验资源。

<!-- anchor: rules -->
## 规则

1. 资源名称必须使用小写字母、数字和连字符，并表达 `<system>-<component>-<purpose>` 或项目内等效稳定结构；不得使用下划线、大写字母、随机后缀作为人工命名主体。
2. namespace 必须表达环境或租户边界，例如 `prod-ordering`、`dev-platform`；不得把多个隔离要求不同的系统混入同一 namespace。
3. 工作负载名称必须与应用组件一致，并可从镜像、日志和监控中关联；不得让 Deployment、Service 和容器名称分别使用不同业务词。
4. Service 名称必须表达被访问的稳定网络入口，且不得包含版本号或临时发布批次；版本差异应通过 selector、label 或流量策略表达。
5. ConfigMap 和 Secret 名称必须表达配置用途和归属组件，Secret 不得在名称中暴露真实凭据、账号、客户或敏感业务内容。
6. 资源 label 必须包含应用、组件和环境等可筛选维度；不得只依赖资源名称完成监控、清理或回滚选择。
7. Helm、Kustomize 或其他工具生成的清单必须有稳定命名前缀和覆盖边界；不得手工编辑生成目录中的产物后再部署。
8. 临时迁移、一次性 Job 或验证资源必须带有明确用途和可清理标识；不得留下无法判断是否仍被使用的孤立资源。

<!-- anchor: examples -->
## 示例

正例：

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ordering-api
  namespace: prod-ordering
  labels:
    app.kubernetes.io/name: ordering
    app.kubernetes.io/component: api
    app.kubernetes.io/part-of: smart-platform
```

反例：

```yaml
kind: Service
metadata:
  name: API_V2_TEMP
  namespace: default
```

<!-- anchor: checklist -->
## 检查清单

- 资源名称是否只使用小写字母、数字和连字符，并表达系统、组件和用途。
- namespace、工作负载、Service、ConfigMap 和 Secret 是否使用一致领域词。
- Service 名称是否保持稳定，未嵌入临时版本或发布批次。
- label 是否足以支持监控、选择器、清理和回滚。
- Secret 名称和清单是否未暴露敏感业务内容。
- 生成清单是否由源配置生成，且没有手工修改生成产物。

<!-- anchor: relations -->
## 相关规范

- rules.container-image
- rules.deploy-rollback
- rules.naming-common

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
