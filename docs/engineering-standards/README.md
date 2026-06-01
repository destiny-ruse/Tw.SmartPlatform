# 成都天问互联工程规范

## 适用范围

《成都天问互联工程规范》适用于公司内部软件研发全生命周期，覆盖需求落地后的设计、开发、测试、评审、构建、发布、运行和工程治理活动。

本规范适用于以下技术栈和项目类型：

- .NET Core 后端服务、后台任务、类库和工具项目。
- Java 后端服务、批处理任务、公共组件和 SDK。
- Python 服务、脚本工具、自动化任务和数据处理任务。
- Vue、TypeScript、JavaScript 前端应用、组件库和管理端项目。
- uni-app 多端应用。
- 涉及 API、数据库、消息、缓存、容器、Kubernetes、CI/CD 和运行观测的软件项目。

本规范不绑定具体代码托管、CI/CD 或运行平台。团队使用 GitLab、GitHub Enterprise、Azure DevOps、Jenkins 或其他平台时，均必须满足本规范定义的工程要求。

## 目标读者

- 开发人员：理解编码、接口、数据、测试、依赖、安全、提交、评审和发布要求。
- 测试人员：理解测试分层、契约测试、测试数据、缺陷回归和质量风险。
- Tech Lead：执行团队工程治理、代码评审、质量改进和项目落地检查。
- 架构师：执行架构评审、跨系统一致性、接口契约、数据边界和高风险变更判断。
- 运维与平台协作人员：理解发布、回滚、运行环境、可观测性和基础设施协作要求。

## 规范用语

本规范使用以下用语表达约束强度：

- **必须**：强制要求，所有适用场景均需遵守。
- **不得**：禁止行为，所有适用场景均禁止出现。
- **应当**：通常情况下需要遵守；确有合理原因不能遵守时，应说明原因和风险。
- **不应**：通常情况下禁止；确有合理原因需要采用时，应说明原因和风险。
- **可以**：允许做法，团队可根据项目情况采用。

## 使用方式

新项目必须在技术方案或项目启动阶段阅读基础规范、协作规范、项目结构规范、通用编码规范和对应语言专项规范。

存量项目应当按照检查清单识别差距，并根据系统风险、业务影响和改造成本安排治理顺序。

代码评审、架构评审、发布评审和故障复盘必须使用本规范作为统一判断依据。通用规范与语言专项规范同时适用时，通用规范定义基础要求，语言专项规范补充技术栈差异。

## 目录导航

### 基础规范

- [工程原则](01-foundation/engineering-principles.md)
- [术语与例外处理](01-foundation/terminology-and-exceptions.md)

### 协作规范

- [仓库组织规范](02-collaboration/repository-organization.md)
- [分支与提交规范](02-collaboration/branching-and-commits.md)
- [代码评审规范](02-collaboration/code-review.md)

### 项目与编码规范

- [项目结构规范](03-project-and-code/project-structure.md)
- [通用编码规范](03-project-and-code/coding-standards.md)
- [API 设计规范](03-project-and-code/api-design.md)
- [数据与数据库规范](03-project-and-code/data-and-database.md)
- [语言专项规范](03-project-and-code/language-specific/README.md)
- [.NET Core 专项规范](03-project-and-code/language-specific/dotnet-core.md)
- [Java 专项规范](03-project-and-code/language-specific/java.md)
- [Python 专项规范](03-project-and-code/language-specific/python.md)
- [Vue 专项规范](03-project-and-code/language-specific/vue.md)
- [uni-app 专项规范](03-project-and-code/language-specific/uni-app.md)
- [TypeScript 专项规范](03-project-and-code/language-specific/typescript.md)
- [JavaScript 专项规范](03-project-and-code/language-specific/javascript.md)

### 质量规范

- [测试规范](04-quality/testing-standards.md)
- [依赖与构建规范](04-quality/dependency-and-build.md)
- [安全开发规范](04-quality/security-standards.md)
- [韧性与可靠性规范](04-quality/resilience-and-reliability.md)

### 交付与运行规范

- [CI/CD 与发布规范](05-delivery-and-operations/ci-cd-and-release.md)
- [可观测性与运维协作规范](05-delivery-and-operations/observability-and-operations.md)
- [运行环境与基础设施规范](05-delivery-and-operations/runtime-and-infrastructure.md)

### 治理规范

- [评审与治理规范](06-governance/review-and-governance.md)

## 例外处理

规范例外必须记录被例外的规则、原因、风险、负责人、缓解措施、确认人和复审日期。例外不得以口头方式替代记录，不得扩散为团队默认实践。

高风险例外必须在评审或发布前完成确认，并在风险消除后关闭。
