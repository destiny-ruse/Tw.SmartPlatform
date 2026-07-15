# 迁移到 2026-07 BuildingBlocks 合并拓扑

本指南帮助仓库内消费方把 16 个已淘汰 PackageId 迁移到 57 个 BuildingBlocks 包的目标拓扑。迁移在采纳前阶段执行，不提供旧包转发壳或类型转发。

## 1. 替换或删除包引用

| 淘汰 PackageId | 迁移目标或删除理由 |
| --- | --- |
| `Tw.Authorization.Abstractions` | 改为 `Tw.Authorization`，权限端口和默认检查器合并到同一能力 |
| `Tw.Domain.Shared` | 删除；业务共享 DTO、枚举和领域契约迁回具体限界上下文 |
| `Tw.Configuration.Json` | 改为 `Tw.Configuration`，JSON 清单与路径校验并入内置配置治理 |
| `Tw.Uow` | 改为 `Tw.Data`，工作单元、事务和 Outbox 边界归数据能力 |
| `Tw.DistributedLocking.Abstractions` | 改为 `Tw.DistributedLocking` |
| `Tw.EventBus.Abstractions` | 改为 `Tw.EventBus` |
| `Tw.Castle.Core` | 删除；入口使用原生管道，应用服务使用应用管线或显式 Decorator，不建立通用动态代理替代包 |
| `Tw.Threading` | 删除；通用异步释放辅助进入 `Tw.Core`，取消令牌改为显式传递 |
| `Tw.Timing` | 删除；改用 .NET `TimeProvider` |
| `Tw.DependencyInjection.Autofac` | 删除；默认容器统一为 Microsoft DI |
| `Tw.Validation.Abstractions` | 改为 `Tw.ExceptionHandling` |
| `Tw.Http.Abstractions` | 改为 `Tw.Http` |
| `Tw.Http.Client` | 改为 `Tw.Http` |
| `Tw.MultiTenancy.Abstractions` | 改为 `Tw.MultiTenancy` |
| `Tw.Sharding.Abstractions` | 改为 `Tw.Sharding` |
| `Tw.AspNetCore.Abstractions` | 改为 `Tw.AspNetCore` |

`Tw.Interception` 从未作为 PackageId 建立，只作为保留禁用名称存在，因此不计入上述 16 个淘汰包，也不得作为 `Tw.Castle.Core` 的替代包新增。

## 2. 更新命名空间和公开 API

- 把 `Tw.Authorization.Abstractions` 引用改为 `Tw.Authorization`
- 删除全局 `Tw.Domain.Shared` 引用，把业务契约放入服务自己的限界上下文
- `Tw.Configuration.Json` 可以作为 `Tw.Configuration` 内部功能命名空间继续使用，但不得继续引用同名旧 PackageId
- 把 `Tw.Uow` 引用改为 `Tw.Data`，并把 `IUnitOfWorkManager`、`SqlSugarUnitOfWorkManager` 及测试替身分别改为对应的 `*UnitOfWorkCoordinator`
- 把 `Tw.DistributedLocking.Abstractions`、`Tw.EventBus.Abstractions`、`Tw.MultiTenancy.Abstractions`、`Tw.Sharding.Abstractions` 和 `Tw.AspNetCore.Abstractions` 改为对应合并包命名空间
- 把验证错误引用迁到 `Tw.ExceptionHandling.Validation`
- 删除 `Tw.Http.Client` 命名空间；请求头传播 API 位于 `Tw.Http.HeaderPropagation`
- 删除 Castle/Autofac 动态代理入口；Web、gRPC、CAP 和 Quartz 分别使用原生拦截点
- 把自有时间抽象改为 `TimeProvider`，并在测试中使用受控 `TimeProvider`
- 显式传递 `CancellationToken`，不得恢复 ambient 取消令牌覆盖

## 3. 更新测试项目

| 旧测试项目 | 新位置或动作 |
| --- | --- |
| `Tw.Domain.Shared.Tests` | 合并到 `Tw.Domain.Tests` |
| `Tw.Configuration.Json.Tests` | 合并到 `Tw.Configuration.Tests` |
| `Tw.Uow.Tests` | 合并到 `Tw.Data.Tests` |
| `Tw.Castle.Core.Tests` | 随动态代理路径删除 |
| `Tw.Threading.Tests` | 通用异步释放测试合并到 `Tw.Core.Tests`；ambient 取消测试删除 |
| `Tw.Timing.Tests` | 删除，时间相关测试改用受控 `TimeProvider` |
| `Tw.DependencyInjection.Autofac.Tests` | Microsoft DI 行为迁入 `Tw.DependencyInjection.Tests`，Autofac 专属测试删除 |
| `Tw.Http.Client.Tests` | 合并到 `Tw.Http.Tests` |

没有独立旧测试项目的其他淘汰包，由目标包现有测试覆盖合并后的契约。

## 4. 验证迁移结果

从仓库根目录运行：

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Architecture/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj
$env:PYTHONPATH = (Resolve-Path tools/src).Path
python -m pytest tools/tests
python -m tw_memory check --root .
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
```

验证结果必须不包含淘汰 PackageId、旧命名空间、兼容壳、类型转发或孤立文档与生成卡片。

## 服务内 `Domain.Shared` 边界

服务本地项目和命名空间，例如模板生成的 `Company.Service.Domain.Shared`，属于该服务限界上下文，不是全局 `Tw.Domain.Shared` Building Block。本次迁移不删除、不重命名，也不禁止这类 service-local `Domain.Shared`。
