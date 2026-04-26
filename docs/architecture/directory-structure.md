# 目录结构说明

本文档从 `.` 开始扫描，根据各级 `README.md` 的一级标题生成目录树说明，并使用正文描述生成目录说明表。

- 生成时间：2026-04-27 01:56:54
- README 数量：48
- 输出文件：`docs/architecture/directory-structure.md`

## 目录树

```text
Tw.SmartPlatform/                            # Tw.SmartPlatform
├─ backend/                                  # 后端目录
│  ├─ dotnet/                                # .NET 后端目录
│  │  ├─ Aspire/                             # .NET Aspire 编排目录
│  │  ├─ BuildingBlocks/                     # .NET 公共构件目录
│  │  │  └─ src/
│  │  │     ├─ Ddd/
│  │  │     │  ├─ Tw.Application/            # 应用层构件目录
│  │  │     │  ├─ Tw.Application.Contracts/  # 应用契约构件目录
│  │  │     │  ├─ Tw.Domain/                 # 领域层构件目录
│  │  │     │  └─ Tw.Domain.Shared/          # 共享领域构件目录
│  │  │     ├─ Tw.AspNetCore/                # ASP.NET Core 集成构件目录
│  │  │     ├─ Tw.Caching/                   # 缓存构件目录
│  │  │     ├─ Tw.Core/                      # 核心基础构件目录
│  │  │     ├─ Tw.DistributedLocking/        # 分布式锁构件目录
│  │  │     ├─ Tw.DocumentProcessing/        # 文档处理构件目录
│  │  │     ├─ Tw.EventBus/                  # 事件总线构件目录
│  │  │     ├─ Tw.MultiTenancy/              # 多租户构件目录
│  │  │     ├─ Tw.PinYinConverter/           # 拼音转换构件目录
│  │  │     ├─ Tw.RemoteService/             # 远程服务构件目录
│  │  │     ├─ Tw.SqlSugar/                  # SqlSugar 数据访问构件目录
│  │  │     ├─ Tw.TextTemplating/            # 文本模板构件目录
│  │  │     └─ Tw.Uow/                       # 工作单元构件目录
│  │  └─ Services/                           # .NET 服务目录
│  │     ├─ Authentication/                  # 认证服务目录
│  │     ├─ Authorization/                   # 授权服务目录
│  │     ├─ Notice/                          # 通知服务目录
│  │     ├─ OSS/                             # 对象存储服务目录
│  │     └─ System/                          # 系统服务目录
│  ├─ java/                                  # Java 后端目录
│  └─ python/                                # Python 后端目录
├─ contracts/                                # 契约目录
│  ├─ openapi/                               # OpenAPI 契约目录
│  └─ protos/                                # Protobuf 契约目录
├─ deploy/                                   # 部署目录
│  ├─ ci-cd/                                 # 持续交付目录
│  ├─ docker-compose/                        # Docker Compose 部署目录
│  └─ k8s-helm/                              # Kubernetes 与 Helm 部署目录
├─ docs/                                     # 文档目录
│  ├─ architecture/                          # 架构文档目录
│  └─ standards/                             # 工程标准目录
├─ frontend/                                 # 前端目录
│  ├─ apps/                                  # 前端应用目录
│  │  ├─ tw.app.owner/                       # 业主端应用目录
│  │  ├─ tw.app.staff/                       # 员工端应用目录
│  │  ├─ tw.web.client/                      # 客户端 Web 应用目录
│  │  ├─ tw.web.ops/                         # 运营 Web 应用目录
│  │  └─ tw.web.portal/                      # 门户 Web 应用目录
│  └─ packages/                              # 前端共享包目录
└─ tools/                                    # 工具目录
```

## 目录说明

| 目录 | 描述 |
| --- | --- |
| `.` | Tw.SmartPlatform 是面向企业端 B/S 架构的智慧管理系统，根目录用于组织后端、前端、契约、部署、文档和工具等工程资产。 |
| `backend` | 本目录用于集中存放后端相关代码，并按 .NET、Java 和 Python 技术栈组织服务与基础能力。 |
| `backend/dotnet` | 本目录用于承载 .NET 后端解决方案、业务服务、公共构件和统一构建配置。 |
| `backend/dotnet/Aspire` | 本目录用于存放 .NET Aspire 相关的本地开发编排、服务启动和运行时集成配置。 |
| `backend/dotnet/BuildingBlocks` | 本目录用于存放 .NET 后端可复用的基础库、领域驱动分层组件和通用技术能力。 |
| `backend/dotnet/BuildingBlocks/src/Ddd/Tw.Application` | 本目录用于承载应用服务实现、用例编排、事务边界和应用层通用基础能力。 |
| `backend/dotnet/BuildingBlocks/src/Ddd/Tw.Application.Contracts` | 本目录用于承载应用服务对外暴露的接口契约、数据传输对象和权限声明基础能力。 |
| `backend/dotnet/BuildingBlocks/src/Ddd/Tw.Domain` | 本目录用于承载领域驱动设计中的实体、聚合、领域服务和领域事件基础能力。 |
| `backend/dotnet/BuildingBlocks/src/Ddd/Tw.Domain.Shared` | 本目录用于承载领域层与应用层共同使用的常量、枚举、值对象和共享领域契约。 |
| `backend/dotnet/BuildingBlocks/src/Tw.AspNetCore` | 本目录用于承载 ASP.NET Core Web 应用的基础集成、中间件、扩展和通用启动支持。 |
| `backend/dotnet/BuildingBlocks/src/Tw.Caching` | 本目录用于承载缓存抽象、缓存策略、缓存提供者集成和通用缓存访问能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.Core` | 本目录用于承载后端通用的核心类型、基础工具、扩展方法和跨模块共享能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.DistributedLocking` | 本目录用于承载分布式锁抽象、并发控制和跨实例互斥执行相关的通用能力实现。 |
| `backend/dotnet/BuildingBlocks/src/Tw.DocumentProcessing` | 本目录用于承载文档生成、解析、转换和处理流程相关的通用能力实现。 |
| `backend/dotnet/BuildingBlocks/src/Tw.EventBus` | 本目录用于承载事件发布订阅、集成事件、领域事件转发和异步解耦通信能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.MultiTenancy` | 本目录用于承载多租户识别、租户上下文、租户隔离和租户配置相关的通用能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.PinYinConverter` | 本目录用于承载中文拼音转换、检索辅助和文本规范化相关的通用能力实现。 |
| `backend/dotnet/BuildingBlocks/src/Tw.RemoteService` | 本目录用于承载远程服务调用、客户端代理、接口转发和服务间通信支持能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.SqlSugar` | 本目录用于承载基于 SqlSugar 的数据访问封装、仓储支持和数据库集成能力。 |
| `backend/dotnet/BuildingBlocks/src/Tw.TextTemplating` | 本目录用于承载文本模板渲染、变量替换和模板化内容生成相关的通用能力实现。 |
| `backend/dotnet/BuildingBlocks/src/Tw.Uow` | 本目录用于承载工作单元模式相关的事务协调、提交边界和基础抽象实现。 |
| `backend/dotnet/Services` | 本目录用于按业务能力划分 .NET 后端服务模块，并统一组织各服务的源码与测试。 |
| `backend/dotnet/Services/Authentication` | 本目录用于承载用户身份认证、登录流程、令牌签发和认证测试相关的服务代码。 |
| `backend/dotnet/Services/Authorization` | 本目录用于承载权限校验、访问控制、角色授权和授权测试相关的服务代码。 |
| `backend/dotnet/Services/Notice` | 本目录用于承载站内通知、消息触达、通知模板和通知测试相关的服务代码。 |
| `backend/dotnet/Services/OSS` | 本目录用于承载对象存储、文件上传下载、资源访问和存储测试相关的服务代码。 |
| `backend/dotnet/Services/System` | 本目录用于承载系统管理、基础配置、平台字典和系统测试相关的服务代码。 |
| `backend/java` | 本目录用于承载 Java 技术栈下的后端服务、组件或集成能力实现。 |
| `backend/python` | 本目录用于承载 Python 技术栈下的后端服务、脚本或实验性能力实现。 |
| `contracts` | 本目录用于集中存放跨服务、跨端协作所需的接口契约与数据交换定义。 |
| `contracts/openapi` | 本目录用于存放基于 OpenAPI 的 HTTP 接口规范、请求响应模型和接口协作说明。 |
| `contracts/protos` | 本目录用于存放基于 Protobuf 的服务通信协议、消息结构和生成代码来源定义。 |
| `deploy` | 本目录用于集中存放项目部署、容器编排、集群发布和持续交付相关资源。 |
| `deploy/ci-cd` | 本目录用于存放持续集成、持续交付、流水线配置和自动化发布相关资源。 |
| `deploy/docker-compose` | 本目录用于存放本地或轻量环境使用的 Docker Compose 编排文件和容器启动配置。 |
| `deploy/k8s-helm` | 本目录用于存放面向 Kubernetes 环境的 Helm Chart、部署模板和集群发布配置。 |
| `docs` | 本目录用于集中存放项目架构、工程标准、目录结构和协作规范等文档资料。 |
| `docs/architecture` | 本目录用于存放系统架构、模块边界、部署拓扑和目录结构相关的说明文档。 |
| `docs/standards` | 本目录用于存放团队工程实践、编码规范、交付约定和质量标准文档。 |
| `frontend` | 本目录用于集中存放前端应用和共享前端包，并承载多端用户界面的工程组织。 |
| `frontend/apps` | 本目录用于集中存放面向不同业务角色和使用场景的前端应用工程。 |
| `frontend/apps/tw.app.owner` | 本目录用于承载面向业主使用场景的前端应用代码与移动端页面资源。 |
| `frontend/apps/tw.app.staff` | 本目录用于承载面向员工使用场景的前端应用代码与移动端页面资源。 |
| `frontend/apps/tw.web.client` | 本目录用于承载面向客户使用场景的 Web 前端应用代码与页面资源。 |
| `frontend/apps/tw.web.ops` | 本目录用于承载面向运营管理场景的 Web 前端应用代码与页面资源。 |
| `frontend/apps/tw.web.portal` | 本目录用于承载面向门户场景的 Web 前端应用代码与页面资源。 |
| `frontend/packages` | 本目录用于存放可被多个前端应用复用的组件、工具库、样式资源和业务封装。 |
| `tools` | 本目录用于存放项目可复用的脚本、辅助工具和自动化处理资源。 |
