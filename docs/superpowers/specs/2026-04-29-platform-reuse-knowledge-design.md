# 平台底层封装复用知识设计

## 状态

用户已在 2026-04-29 确认设计。

## 背景

`Tw.SmartPlatform` 已经开始沉淀跨模块共享的底层封装能力，例如 `.NET` 的 `Tw.Core`、缓存、远程调用、文档处理、文本模板等构件。后续还会出现 Java、Python、Go、前端共享包等多语言底层封装。

当前 Knowledge Compiler 能按目录规则发现模块级漂移，也能通过正式记忆图谱让智能体查询已有模块和能力。但它的粒度仍偏模块级：只要某个底层包已有 `module` 节点，工具就会认为目录结构已对齐，无法判断包内部新增的大量 public API 是否已经可被稳定发现和复用。

`Tw.Core` 已经包含参数检查、扩展方法、反射、加密、哈希、HMAC、RSA、PBKDF2、随机数、基础异常和基础类型等公共能力。`Tw.TestBase` 是测试共享包，包含测试字节、测试流、临时目录、加密测试向量等测试辅助工具。它们如果只藏在源码中，智能体在后续开发中容易重复实现类似方法。

本设计建立一套跨语言、可生成、可查询、可维护的底层封装复用知识机制。

## 目标

1. 让所有语言的底层封装能力都能被 `tw-knowledge-discovery`、`tw-requirement-router` 和人工开发流程稳定发现。
2. 用正式记忆图谱表达跨项目可复用能力和模块关系。
3. 用生成式 public API 轻量索引定位具体复用入口。
4. 只收录 public API，不把 internal 或 private 误提升为跨项目复用契约。
5. 通过注释语义标记增强 `rg` 对 internal/private 关键逻辑的搜索能力。
6. 让 `Tw.TestBase` 支持测试编写时按需发现和复用，但不污染生产侧 public API 轻量索引。
7. 保持生成索引稳定、可验证、低维护成本。

## 非目标

1. 不把每个方法都建成正式 graph 节点。
2. 不索引 internal API 和 private API。
3. 不为语义增强新增 Attribute、Annotation、decorator、Go 结构体标签或运行时读取逻辑。
4. 不把 private API 写入 README 或正式图谱。
5. 不要求所有业务模块都维护复用能力声明。
6. 不在本设计中实现完整语义代码理解，只做 public API 结构化发现和能力关联。
7. 不让 `Tw.TestBase` 进入生产侧 public API 轻量索引。

## 核心决策

### 图谱负责能力决策

正式记忆图谱继续以 `capability` 和 `module` 为中心。它回答：

1. 平台已经有哪些能力。
2. 哪些模块提供这些能力。
3. 什么时候应该复用而不是重复实现。
4. 能力属于生产、测试、平台、集成还是其它领域。

底层封装包必须有 `module` 节点。对外复用能力必须有 `capability` 节点。模块通过 `provides.capabilities` 关联能力。

### public API 轻量索引负责入口定位

public API 轻量索引由工具生成，不手工维护。它回答：

1. 具体 public 类型、函数、方法或扩展方法在哪里。
2. 它属于哪个模块。
3. 它关联哪个 capability。
4. 使用范围是生产还是测试以外的其它范围。
5. 应该阅读哪个源码文件获取详细契约。

索引只收录 public API。internal 和 private 不进入该索引。

### 注释语义标记只增强搜索

internal/private 关键逻辑如果很难通过名称搜索，但又可能影响维护或避免重复实现，可以添加普通注释语义标记。标记只服务 `rg` 搜索，不参与运行时，不进入正式图谱，不进入 public API 索引。

注释语义标记必须遵守各语言注释规范：

1. 使用简体中文。
2. 末尾不使用句号。
3. 说明意图和搜索关键词。
4. 不解释语言语法。
5. 不引入额外代码结构。

示例：

```csharp
// 语义标记：SHA3 回退哈希 流式读取 跨平台兼容
```

```python
# 语义标记：配置解析 环境变量覆盖 默认值合并
```

```go
// 语义标记：请求去重 幂等键 过期清理
```

### Tw.TestBase 使用 README 声明测试共享能力

`backend/dotnet/BuildingBlocks/tests/Tw.TestBase` 是测试共享包，不进入 public API 轻量索引。它在项目根目录维护：

```text
backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md
```

README 通过标记式声明描述测试共享能力。编写测试时，开发者或智能体根据 README 中的标记、关键词和入口文件按需搜索与复用测试辅助工具。

`Tw.TestBase` README 不作为生产能力契约，只服务测试代码复用。

## 覆盖范围

### 生产和框架侧底层封装

以下路径进入正式图谱和 public API 轻量索引机制：

1. `.NET`: `backend/dotnet/BuildingBlocks/src/*`
2. Java: `backend/java/packages/*`
3. Python: `backend/python/packages/*`
4. Go: `backend/go/packages/*`
5. 前端: `frontend/packages/*`

后续新增语言或底层包时，通过 `docs/knowledge/taxonomy.yaml` 增加路径规则纳入。

### 测试共享包

`Tw.TestBase` 单独处理：

1. 不进入 public API 轻量索引。
2. 使用项目根目录 README 声明测试共享能力。
3. 测试编写流程按 README 标记搜索和复用工具。
4. 后续其它语言的测试共享包可沿用同类 README 声明模式。

## 图谱规则

### 模块节点

每个底层封装包必须声明 `module` 节点。节点至少表达：

1. 包的稳定路径。
2. 所属语言栈。
3. 模块类型。
4. 提供的能力关系。
5. 证据路径。

示例：

```yaml
id: backend.dotnet.building-blocks.core
kind: module
module_type: building-block
stack: dotnet
path: backend/dotnet/BuildingBlocks/src/Tw.Core
provides:
  capabilities:
    - backend.capability.core-foundation
    - backend.capability.cryptography
    - backend.capability.reflection
```

### 能力节点

对外复用能力必须声明 `capability` 节点。节点至少表达：

1. 能力名称和摘要。
2. 提供模块。
3. 复用场景。
4. 不要重复实现的约束。
5. 查询别名。

示例能力包括：

1. 核心基础能力。
2. 加密和哈希能力。
3. 反射和类型发现能力。
4. 测试基础能力。
5. 缓存能力。
6. 远程调用能力。
7. 文档处理能力。

### 用途范围

图谱和索引需要区分能力用途范围。初始范围：

1. `production`: 生产代码可用。
2. `test`: 测试代码可用。

`Tw.TestBase` 的测试共享能力属于 `test`，但不进入 public API 轻量索引。

## public API 轻量索引设计

### 生成位置

生成文件放在：

```text
docs/knowledge/generated/api-index/
```

文件命名建议：

```text
docs/knowledge/generated/api-index/{stack}.{module-id}.generated.json
```

聚合索引建议：

```text
docs/knowledge/generated/api-index.generated.json
```

### 记录字段

每条 public API 记录包含：

```json
{
  "id": "dotnet:Tw.Core.Security.Cryptography.AesCryptography.Encrypt",
  "stack": "dotnet",
  "module_id": "backend.dotnet.building-blocks.core",
  "capability_ids": [
    "backend.capability.cryptography"
  ],
  "usage_scope": "production",
  "symbol_kind": "method",
  "namespace": "Tw.Core.Security.Cryptography",
  "type_name": "AesCryptography",
  "member_name": "Encrypt",
  "signature": "Encrypt(string input, byte[] key, byte[]? iv = null, ...)",
  "summary": "使用 AES 加密字符串内容",
  "path": "backend/dotnet/BuildingBlocks/src/Tw.Core/Security/Cryptography/AesCryptography.cs",
  "keywords": [
    "AES",
    "加密",
    "字符串加密"
  ]
}
```

### 收录规则

1. 只收录 public API。
2. 不收录 internal API。
3. 不收录 private API。
4. 不收录 generated、bin、obj、临时文件。
5. 不收录 `Tw.TestBase`。
6. 优先读取公开文档注释作为摘要。
7. 摘要缺失时可以用符号名和能力关系生成保守摘要，并产生诊断提示。
8. 能力归属优先来自模块的 `provides.capabilities`。
9. 无法判断能力归属时记录为空列表，并产生诊断提示。

### 语言解析边界

第一阶段可优先实现 `.NET` public API 解析，因为当前主要缺口集中在 `Tw.Core`。后续逐步扩展 Java、Python、Go 和前端包。

各语言只需解析公共 API 表面，不解析完整语义：

1. `.NET`: public class/interface/record/struct/enum、public method/property/field、public extension method。
2. Java: public class/interface/record/enum、public method/field。
3. Python: 以模块导出的公共符号为主，排除 `_` 前缀，结合 `__all__`。
4. Go: 大写导出标识符。
5. 前端: exported symbols。

## Tw.TestBase README 声明设计

### 文件位置

```text
backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md
```

### 内容结构

README 使用标记式声明，便于测试编写前阅读，也便于 `rg` 搜索。

建议结构：

```markdown
# Tw.TestBase 测试共享能力

## 复用规则

编写测试前先检查本文件，优先复用已有测试辅助工具

## 能力声明

### 测试字节

标记：测试共享能力 测试字节 UTF-8 固定字节
入口：TestBytes.cs
关键词：TestBytes DeterministicBytes Utf8
适用：需要稳定字节数组、UTF-8 编码文本或固定长度测试字节

### 测试流

标记：测试共享能力 测试流 MemoryStream 文本流 字节流
入口：TestStreams.cs
关键词：TestStreams FromText FromBytes
适用：需要从文本或字节快速构造 MemoryStream

### 临时目录

标记：测试共享能力 临时目录 文件写入 自动清理
入口：TemporaryDirectory.cs
关键词：TemporaryDirectory WriteAllText GetPath
适用：需要隔离文件系统副作用并在测试结束后清理

### 加密测试向量

标记：测试共享能力 加密测试向量 固定盐 固定密钥 固定 IV
入口：CryptoTestVectors.cs
关键词：CryptoTestVectors Salt Key128 Key256 Iv128
适用：需要稳定加密测试输入，避免测试中生成临时密钥
```

### 使用约束

1. README 声明不进入 public API 轻量索引。
2. README 声明只服务测试代码。
3. 测试编写时优先按标记和关键词搜索 `Tw.TestBase`。
4. 新增测试共享工具时必须更新 README。
5. README 中声明的入口文件必须存在。

## 工具行为

### knowledge.py scan

`scan` 应发现底层封装包缺少图谱节点的问题。对生产和框架侧底层包：

1. 缺少 `module` 节点时报诊断。
2. 模块可能暴露可复用能力但没有 capability 关系时报诊断。
3. 不要求 `Tw.TestBase` 进入 public API 索引。
4. `Tw.TestBase` 缺少 README 或 README 缺少测试共享能力声明时报诊断。

### knowledge.py generate

`generate` 应生成：

1. 现有 memory/index/edges 文件。
2. public API 轻量索引。

`generate` 不为 `Tw.TestBase` 生成 public API 索引。

### knowledge.py check

`check` 应验证：

1. 图谱节点仍然有效。
2. public API 轻量索引是最新的。
3. public API 索引没有包含 internal/private API。
4. public API 索引没有包含 `Tw.TestBase`。
5. `Tw.TestBase/README.md` 存在并包含测试共享能力声明。
6. README 声明的入口文件存在。
7. 注释语义标记格式符合约定。

### tw-knowledge-discovery

发现流程：

1. 先查询正式图谱 capability/module。
2. 再读取 public API 轻量索引定位入口。
3. 如果是测试编写场景，读取 `Tw.TestBase/README.md`。
4. 根据 README 的标记和关键词，用 `rg` 搜索测试辅助工具。
5. 输出建议时区分生产能力和测试共享能力。

### tw-requirement-router

路由流程：

1. 对框架层或公共能力需求，先查询 capability。
2. 如果命中底层封装能力，建议复用对应模块。
3. 如果需要具体调用入口，读取 public API 轻量索引。
4. 如果需求是测试编写，额外提示检查 `Tw.TestBase/README.md`。
5. 如果未命中但路径属于底层封装范围，提示可能需要新增图谱能力或 public API 索引覆盖。

## 注释语义标记规范

### 适用场景

1. internal/private 中的关键算法。
2. 兼容性分支。
3. 安全或性能取舍。
4. 测试辅助逻辑。
5. 难以从名称直接搜索到的实现。

### 非适用场景

1. 普通语法说明。
2. 已经能从 public API 和图谱发现的能力。
3. private API 的复用契约声明。
4. README 替代品。

### 格式

统一使用：

```text
语义标记：<关键词1> <关键词2> <关键词3>
```

各语言按本语言注释语法写入：

```csharp
// 语义标记：HMAC SHA3 回退实现 流式哈希
```

```java
// 语义标记：请求签名 时间戳校验 重放保护
```

```python
# 语义标记：配置合并 环境变量覆盖 默认值
```

```go
// 语义标记：幂等键 请求去重 过期清理
```

```ts
// 语义标记：表单校验 字段路径 错误映射
```

## 诊断设计

新增或扩展诊断：

1. `knowledge.missing-capability-link`
   - 模块是底层封装，但没有 `provides.capabilities`
   - 级别：warning

2. `knowledge.api-index-stale`
   - public API 轻量索引不是最新
   - 级别：error

3. `knowledge.api-index-unassigned`
   - public API 无法归属到 capability
   - 级别：warning

4. `knowledge.api-index-invalid-scope`
   - 索引包含不允许的路径，例如 `Tw.TestBase`
   - 级别：error

5. `knowledge.testbase-readme-missing`
   - `Tw.TestBase/README.md` 不存在
   - 级别：warning

6. `knowledge.testbase-readme-invalid`
   - README 缺少能力声明或入口文件不存在
   - 级别：warning

7. `knowledge.semantic-marker-invalid`
   - 注释语义标记格式不符合约定
   - 级别：warning

## 查询体验

### 生产能力查询

查询“加密”时应优先返回：

1. 加密 capability。
2. 提供该能力的模块，例如 `Tw.Core`。
3. public API 入口，例如 AES、DES、TripleDES、RSA、PBKDF2、哈希和 HMAC。
4. 建议阅读的源码文件。

### 测试能力查询

查询“临时目录测试”时应返回：

1. 测试共享能力说明。
2. `backend/dotnet/BuildingBlocks/tests/Tw.TestBase/README.md`。
3. 搜索关键词。
4. 入口文件 `TemporaryDirectory.cs`。

不返回生产侧 public API 索引记录。

### 未命中查询

如果查询属于底层封装能力但没有图谱或 API 入口，工具应提示：

1. 可能已有代码但未进入知识体系。
2. 建议运行 `rg` 搜索。
3. 如果确认是公共能力，应补 module/capability 或 public API 注释。

## 验证策略

### 单元测试

1. taxonomy 路径规则测试。
2. module 与 capability 关联诊断测试。
3. public API 轻量索引生成测试。
4. public/internal/private 过滤测试。
5. `Tw.TestBase` 排除 API 索引测试。
6. `Tw.TestBase/README.md` 声明校验测试。
7. 注释语义标记格式测试。
8. query 命中 capability/module/API 入口测试。

### 集成验证

运行：

```powershell
python tools\knowledge\knowledge.py generate
python tools\knowledge\knowledge.py check
python tools\knowledge\knowledge.py scan
```

典型查询：

```powershell
python tools\knowledge\knowledge.py query --text "加密" --limit 5
python tools\knowledge\knowledge.py query --text "反射 类型查找" --limit 5
python tools\knowledge\knowledge.py query --text "临时目录测试" --limit 5
python tools\knowledge\knowledge.py query --text "测试字节" --limit 5
```

### 回归目标

1. 查询生产底层能力时能返回 capability、module 和 public API 入口。
2. 查询测试共享能力时指向 `Tw.TestBase/README.md`。
3. public API 索引不包含 internal/private。
4. public API 索引不包含 `Tw.TestBase`。
5. 缺少 capability 关系时有诊断。
6. 语义标记不会改变运行时行为。

## 迁移策略

1. 更新 taxonomy 和 Knowledge Compiler 规则，识别底层封装模块能力关联缺口。
2. 为 `Tw.Core` 补齐 capability 关系和能力摘要。
3. 创建 `Tw.TestBase/README.md` 测试共享能力声明。
4. 实现 `.NET` public API 轻量索引生成。
5. 扩展 `generate/check/scan/query` 使用 API 索引。
6. 更新 `tw-knowledge-discovery` 和 `tw-requirement-router` 说明，使其优先使用图谱和 public API 轻量索引。
7. 为 Java、Python、Go 和前端包逐步接入 public API 解析。
8. 补充查询回归测试和标准检查。

## 风险与缓解

### 索引噪音

public API 数量增长后，查询可能返回过多入口。

缓解：先返回 capability/module，再按 capability 过滤 public API。限制默认结果数量，提供 read target 而不是展开长内容。

### 注释质量影响检索

public API 注释过泛会导致索引摘要不清晰。

缓解：对缺少摘要或摘要过短的 public API 产生诊断。继续沿用简体中文注释规范。

### 生成文件频繁变化

API 改动会导致索引变化。

缓解：生成字段保持最小集合，排序稳定，不记录易变实现细节。

### internal/private 被误当作契约

语义标记可能被误读为复用契约。

缓解：设计明确语义标记只用于 `rg` 搜索，不进入图谱和 API 索引。发现建议必须区分 public API 和源码内部实现。

### 测试工具污染生产推荐

`Tw.TestBase` 如果进入全局索引，可能被生产代码误用。

缓解：明确排除 `Tw.TestBase` public API 索引，只通过 README 在测试场景下发现。

## 成功标准

1. 底层封装能力能通过 capability 查询发现。
2. 生产侧 public API 能通过生成索引定位到源码入口。
3. `Tw.TestBase` 测试共享能力能通过 README 声明和 `rg` 搜索发现。
4. internal/private 不进入正式复用契约。
5. 新增底层封装包时，缺少图谱或能力关系会被工具提示。
6. 智能体在实现新需求前能稳定发现已有底层封装，减少重复实现。
