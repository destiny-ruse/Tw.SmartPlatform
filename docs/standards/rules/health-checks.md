---
id: rules.health-checks
title: 健康检查规范
doc_type: rule
status: active
version: 1.1.0
owners: [architecture-team]
roles: [backend, qa, devops, ai]
stacks: [dotnet, java, python]
tags: [observability, operations]
summary: 规定存活、就绪和依赖检查的语义。
machine_rules: []
supersedes: []
superseded_by:
review_after: 2026-10-27
---

# 健康检查规范

<!-- anchor: goal -->
## 目标

健康检查规范用于降低编排平台误杀进程、未就绪实例过早接流和依赖故障被误判为进程死亡的风险。服务必须清晰区分 liveness、readiness 和 startup 语义，并把依赖健康边界表达为可验证接口。这样部署、回滚、SLO 和故障处置可以基于一致的健康信号做决策。

<!-- anchor: scope -->
## 适用范围

本规范适用于 `apps/`、`services/`、`src/`、`backend/` 中 HTTP API、gRPC 服务、消息消费者、批处理任务、后台作业、网关适配器、容器探针配置、部署清单和运维验证脚本。它覆盖 liveness、readiness、startup、依赖检查、检查超时、返回载荷、状态码和部署后验证。它不适用于离线一次性脚本、开发机手动调试端点或不参与流量调度的内部诊断页面。

<!-- anchor: rules -->
## 规则

1. 服务必须区分 liveness、readiness 和 startup 检查；不得用同一个端点同时表达进程存活、启动完成和可接收流量。
2. Liveness 只能验证进程是否可继续运行和主事件循环是否响应；不得因为数据库、缓存、第三方 API 或消息队列短暂失败而触发进程重启。
3. Readiness 必须覆盖接流所需的本地初始化、配置加载、连接池状态和关键依赖边界；依赖不可用时必须返回不可接流状态。
4. Startup 必须覆盖冷启动、迁移等待、缓存预热或模型加载等启动阶段；不得在启动未完成时让 readiness 提前成功。
5. 健康检查必须设置调用超时、轻量查询和稳定状态码；不得执行昂贵全表扫描、写操作、外部副作用或长时间阻塞任务。
6. 依赖检查必须按关键依赖和可降级依赖分层；可降级依赖失败可以影响详情和指标，但不得在服务仍可按设计降级服务时错误摘流。
7. 健康检查响应可以包含组件状态和原因码，但不得暴露密钥、连接串、完整异常堆栈、PII 或内部拓扑细节。
8. 部署清单、运维脚本和发布验证必须引用正确探针端点；不得只依赖首页、文档页或任意业务接口判断服务健康。

<!-- anchor: examples -->
## 示例

正例：

```yaml
probes:
  liveness: /health/live
  readiness: /health/ready
  startup: /health/startup
readinessDependencies:
  required: [database, message-broker]
  degraded: [search-index]
```

反例：

```csharp
app.MapGet("/health/live", async db => {
    await db.ExecuteAsync("select count(*) from orders");
    await paymentClient.PingAsync();
    return Results.Ok("alive");
});
```

<!-- anchor: checklist -->
## 检查清单

- 是否分别定义 liveness、readiness 和 startup，并说明各自语义。
- Liveness 是否避免检查外部依赖，防止依赖故障触发进程重启。
- Readiness 是否覆盖接流所需初始化、配置和关键依赖。
- 健康检查是否轻量、只读、有超时，并避免副作用和敏感信息泄露。
- 关键依赖和可降级依赖是否分层，降级状态是否有指标或日志支持。
- 部署清单和发布验证是否引用正确健康端点。

<!-- anchor: relations -->
## 相关规范

- rules.metrics
- rules.slo
- rules.deploy-rollback
- rules.resilience

<!-- anchor: changelog -->
## 变更记录

| 版本 | 日期 | 说明 |
| --- | --- | --- |
| 1.1.0 | 2026-04-27 | 补充执行级规则、示例、检查清单和相关规范。 |
| 1.0.0 | 2026-04-27 | 建立初始规范。 |
