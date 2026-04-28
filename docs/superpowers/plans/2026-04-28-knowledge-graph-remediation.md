# Knowledge Graph 整改 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除 `knowledge.py scan` 输出的 2 个 ERROR（OSS / System 服务目录无 module 节点）和 12 个 WARN（BuildingBlock 目录无 capability 或 module 节点）。

**Architecture:** 分三类处理。① 两个服务目录：创建 module 节点。② 九个有跨服务复用价值的 BuildingBlock：各创建一对 capability + module 节点，module 通过 `provides.capabilities` 关联 capability。③ 三个内部基础构件（Ddd / Core / Uow）：仅创建 module 节点，不声明 capability。全部创建后运行 `generate + check + scan` 验证。

**Tech Stack:** Python 3（标准库），`tools/knowledge/knowledge.py`，YAML（手工编辑），`python -m unittest tests.knowledge.test_knowledge_tool`。

---

## 文件清单

**新增（module 节点）：**
- `docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.services.system.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.caching.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.event-bus.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.distributed-locking.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.multi-tenancy.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.asp-net-core.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.sql-sugar.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.document-processing.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.text-templating.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.pin-yin-converter.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.ddd.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`
- `docs/knowledge/graph/modules/backend.dotnet.building-blocks.uow.yaml`

**新增（capability 节点，9 个）：**
- `docs/knowledge/graph/capabilities/backend.capability.caching.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.event-bus.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.distributed-locking.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.multi-tenancy.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.asp-net-core.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.sql-sugar.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.document-processing.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.text-templating.yaml`
- `docs/knowledge/graph/capabilities/backend.capability.pin-yin-converter.yaml`

**自动修改（`generate` 命令重建）：**
- `docs/knowledge/generated/index.generated.json`
- `docs/knowledge/generated/memory.generated.json`
- `docs/knowledge/generated/edges.generated.json`
- `docs/knowledge/generated/_index/**`（所有分片索引）

---

## Task 1：修复 ERROR — 创建 OSS 和 System 服务 module 节点

**Files:**
- Create: `docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.services.system.yaml`

- [ ] **Step 1：用 init 生成 OSS 模板**

```powershell
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.services.oss
```

Expected：`Created docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml`

- [ ] **Step 2：覆盖 OSS 节点内容**

将 `docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml` 内容替换为：

```yaml
schema_version: 1.0.0
id: backend.dotnet.services.oss
kind: module
name: 对象存储服务
status: active
summary: 承载对象存储、文件上传下载、资源访问和存储测试相关服务代码。
owners:
  - platform
tags:
  - backend
  - dotnet
  - oss
module_type: microservice
stack: dotnet
path: backend/dotnet/Services/OSS
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml
  evidence:
    - backend/dotnet/Services/OSS/README.md
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 3：用 init 生成 System 模板**

```powershell
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.services.system
```

Expected：`Created docs/knowledge/graph/modules/backend.dotnet.services.system.yaml`

- [ ] **Step 4：覆盖 System 节点内容**

将 `docs/knowledge/graph/modules/backend.dotnet.services.system.yaml` 内容替换为：

```yaml
schema_version: 1.0.0
id: backend.dotnet.services.system
kind: module
name: 系统服务
status: active
summary: 承载系统管理、基础配置、平台字典和系统测试相关服务代码。
owners:
  - platform
tags:
  - backend
  - dotnet
  - system
module_type: microservice
stack: dotnet
path: backend/dotnet/Services/System
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.services.system.yaml
  evidence:
    - backend/dotnet/Services/System/README.md
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 5：校验节点并生成索引**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py generate
```

Expected：两条命令均输出 `OK ...`，无 ERROR。

- [ ] **Step 6：Commit**

```powershell
git add docs/knowledge/graph/modules/backend.dotnet.services.oss.yaml `
        docs/knowledge/graph/modules/backend.dotnet.services.system.yaml `
        docs/knowledge/generated/
git commit -m "feat: add module nodes for OSS and System services"
```

---

## Task 2：创建基础设施 Capability 节点（Caching / EventBus / DistributedLocking）

**Files:**
- Create: `docs/knowledge/graph/capabilities/backend.capability.caching.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.caching.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.event-bus.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.event-bus.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.distributed-locking.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.distributed-locking.yaml`

- [ ] **Step 1：初始化 Caching 节点**

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.caching
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.caching
```

Expected：两个 `Created ...` 输出。

- [ ] **Step 2：覆盖 Caching capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.caching.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.caching
kind: capability
name: 缓存能力
status: active
summary: 提供缓存抽象、缓存策略和通用缓存访问接口，屏蔽底层缓存提供者差异。
owners:
  - platform
tags:
  - backend
  - caching
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.caching
reuse:
  use_when:
    - 需要缓存数据或减少重复计算时
  do_not_reimplement:
    - 不要在服务中直接引用特定缓存库，应通过 Tw.Caching 提供的抽象接口使用
aliases:
  - 缓存
  - cache
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.caching.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Caching
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 3：覆盖 Caching module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.caching.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.caching
kind: module
name: 缓存构件
status: active
summary: 承载缓存抽象、缓存策略、缓存提供者集成和通用缓存访问能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - caching
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.Caching
provides:
  capabilities:
    - backend.capability.caching
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.caching.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Caching
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 4：初始化 EventBus 节点**

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.event-bus
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.event-bus
```

- [ ] **Step 5：覆盖 EventBus capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.event-bus.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.event-bus
kind: capability
name: 事件总线能力
status: active
summary: 提供事件发布订阅、集成事件和领域事件转发能力，支持服务间异步解耦通信。
owners:
  - platform
tags:
  - backend
  - event-bus
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.event-bus
reuse:
  use_when:
    - 需要跨服务异步通信或领域事件转发时
  do_not_reimplement:
    - 不要在服务中直接依赖 MQ 客户端，应通过 Tw.EventBus 提供的接口发布和订阅事件
aliases:
  - 事件
  - event
  - 消息队列
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.event-bus.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.EventBus
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 6：覆盖 EventBus module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.event-bus.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.event-bus
kind: module
name: 事件总线构件
status: active
summary: 承载事件发布订阅、集成事件、领域事件转发和异步解耦通信能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - event-bus
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.EventBus
provides:
  capabilities:
    - backend.capability.event-bus
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.event-bus.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.EventBus
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 7：初始化 DistributedLocking 节点**

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.distributed-locking
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.distributed-locking
```

- [ ] **Step 8：覆盖 DistributedLocking capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.distributed-locking.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.distributed-locking
kind: capability
name: 分布式锁能力
status: active
summary: 提供分布式锁抽象和跨实例互斥执行能力，用于并发控制场景。
owners:
  - platform
tags:
  - backend
  - distributed-locking
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.distributed-locking
reuse:
  use_when:
    - 需要跨实例互斥执行某段逻辑时
  do_not_reimplement:
    - 不要在服务中自行实现分布式锁逻辑，应通过 Tw.DistributedLocking 提供的接口获取锁
aliases:
  - 分布式锁
  - 互斥锁
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.distributed-locking.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.DistributedLocking
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 9：覆盖 DistributedLocking module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.distributed-locking.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.distributed-locking
kind: module
name: 分布式锁构件
status: active
summary: 承载分布式锁抽象、并发控制和跨实例互斥执行相关的通用能力实现。
owners:
  - platform
tags:
  - backend
  - dotnet
  - distributed-locking
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.DistributedLocking
provides:
  capabilities:
    - backend.capability.distributed-locking
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.distributed-locking.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.DistributedLocking
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 10：校验并生成索引**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py generate
```

Expected：均输出 `OK ...`，无 ERROR。

- [ ] **Step 11：Commit**

```powershell
git add docs/knowledge/graph/capabilities/backend.capability.caching.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.caching.yaml `
        docs/knowledge/graph/capabilities/backend.capability.event-bus.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.event-bus.yaml `
        docs/knowledge/graph/capabilities/backend.capability.distributed-locking.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.distributed-locking.yaml `
        docs/knowledge/generated/
git commit -m "feat: add capability nodes for caching, event-bus, distributed-locking"
```

---

## Task 3：创建平台能力 Capability 节点（MultiTenancy / AspNetCore / SqlSugar）

**Files:**
- Create: `docs/knowledge/graph/capabilities/backend.capability.multi-tenancy.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.multi-tenancy.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.asp-net-core.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.asp-net-core.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.sql-sugar.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.sql-sugar.yaml`

- [ ] **Step 1：初始化三个 capability + module**

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.multi-tenancy
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.multi-tenancy
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.asp-net-core
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.asp-net-core
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.sql-sugar
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.sql-sugar
```

Expected：6 个 `Created ...` 输出。

- [ ] **Step 2：覆盖 MultiTenancy capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.multi-tenancy.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.multi-tenancy
kind: capability
name: 多租户能力
status: active
summary: 提供租户识别、租户上下文传递和租户隔离能力，支持多租户 SaaS 架构。
owners:
  - platform
tags:
  - backend
  - multi-tenancy
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.multi-tenancy
reuse:
  use_when:
    - 需要获取当前租户信息或按租户隔离数据时
  do_not_reimplement:
    - 不要在服务中自行解析租户标识，应通过 Tw.MultiTenancy 提供的租户上下文接口获取
aliases:
  - 租户
  - 多租户
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.multi-tenancy.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.MultiTenancy
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 3：覆盖 MultiTenancy module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.multi-tenancy.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.multi-tenancy
kind: module
name: 多租户构件
status: active
summary: 承载多租户识别、租户上下文、租户隔离和租户配置相关的通用能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - multi-tenancy
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.MultiTenancy
provides:
  capabilities:
    - backend.capability.multi-tenancy
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.multi-tenancy.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.MultiTenancy
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 4：覆盖 AspNetCore capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.asp-net-core.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.asp-net-core
kind: capability
name: ASP.NET Core 集成能力
status: active
summary: 提供 Web 应用基础集成、中间件注册和通用启动配置支持，统一后端服务的 Web 层初始化方式。
owners:
  - platform
tags:
  - backend
  - asp-net-core
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.asp-net-core
reuse:
  use_when:
    - 需要注册中间件、配置 Web 主机或使用统一的服务启动约定时
  do_not_reimplement:
    - 不要在各服务中重复配置 Web 层基础设施，应通过 Tw.AspNetCore 提供的扩展方法注册
aliases:
  - Web 宿主
  - 中间件
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.asp-net-core.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.AspNetCore
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 5：覆盖 AspNetCore module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.asp-net-core.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.asp-net-core
kind: module
name: ASP.NET Core 集成构件
status: active
summary: 承载 ASP.NET Core Web 应用的基础集成、中间件、扩展和通用启动支持。
owners:
  - platform
tags:
  - backend
  - dotnet
  - asp-net-core
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.AspNetCore
provides:
  capabilities:
    - backend.capability.asp-net-core
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.asp-net-core.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.AspNetCore
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 6：覆盖 SqlSugar capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.sql-sugar.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.sql-sugar
kind: capability
name: SqlSugar 数据访问能力
status: active
summary: 提供基于 SqlSugar 的数据访问封装和仓储支持，统一后端服务的数据库访问方式。
owners:
  - platform
tags:
  - backend
  - sql-sugar
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.sql-sugar
reuse:
  use_when:
    - 需要访问关系型数据库或使用仓储模式时
  do_not_reimplement:
    - 不要在服务中直接配置 SqlSugar 客户端，应通过 Tw.SqlSugar 提供的仓储基类和扩展接口使用
aliases:
  - 数据访问
  - ORM
  - 仓储
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.sql-sugar.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.SqlSugar
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 7：覆盖 SqlSugar module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.sql-sugar.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.sql-sugar
kind: module
name: SqlSugar 数据访问构件
status: active
summary: 承载基于 SqlSugar 的数据访问封装、仓储支持和数据库集成能力。
owners:
  - platform
tags:
  - backend
  - dotnet
  - sql-sugar
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.SqlSugar
provides:
  capabilities:
    - backend.capability.sql-sugar
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.sql-sugar.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.SqlSugar
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 8：校验并生成索引**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py generate
```

Expected：均输出 `OK ...`，无 ERROR。

- [ ] **Step 9：Commit**

```powershell
git add docs/knowledge/graph/capabilities/backend.capability.multi-tenancy.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.multi-tenancy.yaml `
        docs/knowledge/graph/capabilities/backend.capability.asp-net-core.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.asp-net-core.yaml `
        docs/knowledge/graph/capabilities/backend.capability.sql-sugar.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.sql-sugar.yaml `
        docs/knowledge/generated/
git commit -m "feat: add capability nodes for multi-tenancy, asp-net-core, sql-sugar"
```

---

## Task 4：创建工具类 Capability 节点（DocumentProcessing / TextTemplating / PinYinConverter）

**Files:**
- Create: `docs/knowledge/graph/capabilities/backend.capability.document-processing.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.document-processing.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.text-templating.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.text-templating.yaml`
- Create: `docs/knowledge/graph/capabilities/backend.capability.pin-yin-converter.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.pin-yin-converter.yaml`

- [ ] **Step 1：初始化三个 capability + module**

```powershell
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.document-processing
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.document-processing
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.text-templating
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.text-templating
python tools/knowledge/knowledge.py init --kind capability --id backend.capability.pin-yin-converter
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.pin-yin-converter
```

Expected：6 个 `Created ...` 输出。

- [ ] **Step 2：覆盖 DocumentProcessing capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.document-processing.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.document-processing
kind: capability
name: 文档处理能力
status: active
summary: 提供文档生成、解析、格式转换和处理流程能力，支持 PDF、Word 等常见文档格式操作。
owners:
  - platform
tags:
  - backend
  - document-processing
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.document-processing
reuse:
  use_when:
    - 需要生成、解析或转换文档格式时
  do_not_reimplement:
    - 不要在服务中直接依赖文档库，应通过 Tw.DocumentProcessing 提供的处理接口操作文档
aliases:
  - 文档生成
  - PDF
  - 报表
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.document-processing.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.DocumentProcessing
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 3：覆盖 DocumentProcessing module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.document-processing.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.document-processing
kind: module
name: 文档处理构件
status: active
summary: 承载文档生成、解析、转换和处理流程相关的通用能力实现。
owners:
  - platform
tags:
  - backend
  - dotnet
  - document-processing
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.DocumentProcessing
provides:
  capabilities:
    - backend.capability.document-processing
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.document-processing.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.DocumentProcessing
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 4：覆盖 TextTemplating capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.text-templating.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.text-templating
kind: capability
name: 文本模板能力
status: active
summary: 提供文本模板渲染、变量替换和模板化内容生成能力，用于通知、邮件和动态文本场景。
owners:
  - platform
tags:
  - backend
  - text-templating
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.text-templating
reuse:
  use_when:
    - 需要基于模板动态生成文本内容时（如通知消息、邮件正文）
  do_not_reimplement:
    - 不要在服务中自行拼接动态文本，应通过 Tw.TextTemplating 提供的模板渲染接口生成内容
aliases:
  - 模板
  - 渲染
  - 通知文本
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.text-templating.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.TextTemplating
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 5：覆盖 TextTemplating module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.text-templating.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.text-templating
kind: module
name: 文本模板构件
status: active
summary: 承载文本模板渲染、变量替换和模板化内容生成相关的通用能力实现。
owners:
  - platform
tags:
  - backend
  - dotnet
  - text-templating
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.TextTemplating
provides:
  capabilities:
    - backend.capability.text-templating
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.text-templating.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.TextTemplating
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 6：覆盖 PinYinConverter capability 内容**

`docs/knowledge/graph/capabilities/backend.capability.pin-yin-converter.yaml`：

```yaml
schema_version: 1.0.0
id: backend.capability.pin-yin-converter
kind: capability
name: 拼音转换能力
status: active
summary: 提供中文拼音转换、检索辅助和文本规范化能力，支持拼音搜索和排序场景。
owners:
  - platform
tags:
  - backend
  - pin-yin-converter
  - platform
domain: platform
provided_by:
  modules:
    - backend.dotnet.building-blocks.pin-yin-converter
reuse:
  use_when:
    - 需要将中文转换为拼音用于检索、排序或文本规范化时
  do_not_reimplement:
    - 不要在服务中引入独立的拼音库，应通过 Tw.PinYinConverter 提供的转换接口使用
aliases:
  - 拼音
  - 检索
source:
  declared_in: docs/knowledge/graph/capabilities/backend.capability.pin-yin-converter.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.PinYinConverter
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 7：覆盖 PinYinConverter module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.pin-yin-converter.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.pin-yin-converter
kind: module
name: 拼音转换构件
status: active
summary: 承载中文拼音转换、检索辅助和文本规范化相关的通用能力实现。
owners:
  - platform
tags:
  - backend
  - dotnet
  - pin-yin-converter
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.PinYinConverter
provides:
  capabilities:
    - backend.capability.pin-yin-converter
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.pin-yin-converter.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.PinYinConverter
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 8：校验并生成索引**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py generate
```

Expected：均输出 `OK ...`，无 ERROR。

- [ ] **Step 9：Commit**

```powershell
git add docs/knowledge/graph/capabilities/backend.capability.document-processing.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.document-processing.yaml `
        docs/knowledge/graph/capabilities/backend.capability.text-templating.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.text-templating.yaml `
        docs/knowledge/graph/capabilities/backend.capability.pin-yin-converter.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.pin-yin-converter.yaml `
        docs/knowledge/generated/
git commit -m "feat: add capability nodes for document-processing, text-templating, pin-yin-converter"
```

---

## Task 5：创建内部基础构件 Module 节点（Ddd / Core / Uow）

这三个构件不对外提供可复用能力，只需声明 module 节点使 `scan` 知道它们已被记录。

**⚠️ 注意**：`Ddd` 目录不带 `Tw.` 前缀，`init` 会错误推断 `path` 为 `backend/dotnet/BuildingBlocks/src/Tw.Ddd`，需手工更正为 `backend/dotnet/BuildingBlocks/src/Ddd`。

**Files:**
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.ddd.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`
- Create: `docs/knowledge/graph/modules/backend.dotnet.building-blocks.uow.yaml`

- [ ] **Step 1：初始化三个内部构件 module 节点**

```powershell
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.ddd
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.core
python tools/knowledge/knowledge.py init --kind module --id backend.dotnet.building-blocks.uow
```

Expected：3 个 `Created ...` 输出。

- [ ] **Step 2：覆盖 Ddd module 内容（注意修正 path 为无前缀的 Ddd）**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.ddd.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.ddd
kind: module
name: DDD 基础构件
status: active
summary: 承载领域驱动设计的基础抽象，包括实体、聚合根、值对象和领域事件基类，供各服务领域层继承使用。
owners:
  - platform
tags:
  - backend
  - dotnet
  - ddd
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Ddd
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.ddd.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Ddd
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 3：覆盖 Core module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.core
kind: module
name: 核心基础构件
status: active
summary: 承载后端通用的核心类型、基础工具、扩展方法和跨模块共享能力，是所有服务的基础依赖。
owners:
  - platform
tags:
  - backend
  - dotnet
  - core
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.Core
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Core
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 4：覆盖 Uow module 内容**

`docs/knowledge/graph/modules/backend.dotnet.building-blocks.uow.yaml`：

```yaml
schema_version: 1.0.0
id: backend.dotnet.building-blocks.uow
kind: module
name: 工作单元构件
status: active
summary: 承载工作单元模式相关的事务协调、提交边界和基础抽象实现，供数据访问层使用。
owners:
  - platform
tags:
  - backend
  - dotnet
  - uow
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.Uow
source:
  declared_in: docs/knowledge/graph/modules/backend.dotnet.building-blocks.uow.yaml
  evidence:
    - backend/dotnet/BuildingBlocks/src/Tw.Uow
provenance:
  created_by: human
  created_at: 2026-04-28
  updated_by: human
  updated_at: 2026-04-28
```

- [ ] **Step 5：校验并生成索引**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
python tools/knowledge/knowledge.py generate
```

Expected：均输出 `OK ...`，无 ERROR。

- [ ] **Step 6：Commit**

```powershell
git add docs/knowledge/graph/modules/backend.dotnet.building-blocks.ddd.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.core.yaml `
        docs/knowledge/graph/modules/backend.dotnet.building-blocks.uow.yaml `
        docs/knowledge/generated/
git commit -m "feat: add module nodes for internal building blocks ddd, core, uow"
```

---

## Task 6：全量验证

- [ ] **Step 1：运行单元测试**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python -m unittest tests.knowledge.test_knowledge_tool
```

Expected：`exit code 0`，最后一行类似 `Ran N tests in X.XXXs`，无 FAIL/ERROR。

- [ ] **Step 2：运行 scan，确认 0 ERROR、0 WARN**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py scan
echo "Exit: $LASTEXITCODE"
```

Expected：`Exit: 0`，无任何诊断输出（或仅输出 `OK ...`）。

- [ ] **Step 3：运行 check 确认图谱一致**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py check
```

Expected：`OK knowledge checks passed`。

- [ ] **Step 4：运行 query 确认索引可用**

```powershell
$env:PYTHONIOENCODING = "utf-8"
python tools/knowledge/knowledge.py query --text "缓存" --limit 3
python tools/knowledge/knowledge.py query --text "数据访问" --limit 3
```

Expected：第一条返回 `backend.capability.caching`，第二条返回 `backend.capability.sql-sugar`。

- [ ] **Step 5：Commit 验证结果（如 generated 有更新）**

```powershell
git status
# 若 docs/knowledge/generated/ 有变动：
git add docs/knowledge/generated/
git commit -m "chore: regenerate knowledge index after full graph remediation"
```
