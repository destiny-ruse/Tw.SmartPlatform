# Tw AI 工具开发记忆层设计

**日期**：2026-04-30
**状态**：设计已确认，等待用户审阅落盘稿后进入实现计划。
**方案选择**：采用 `.tw-memory` + `tools/tw-memory` + thin skills，并预留可插拔检索后端。
**文档类型**：解释型设计文档，面向平台维护者、AI 工具维护者和后续实现代理。

---

## 1. 背景

当前仓库是多语言、多服务、多框架的工程大仓。公司内部框架会持续演进，微服务分布在不同目录或仓库中，各语言体系都有包管理、内部二次封装框架、公共方法、工程规范和使用手册。

AI 工具参与开发时，需要在改代码前知道：

1. 当前任务属于哪个语言、框架、服务或规范域。
2. 公司内部已经有哪些二次封装能力可以复用。
3. 相关工程规范和中文使用手册在哪里。
4. 哪些文件是事实来源，哪些只是检索索引。
5. 如何用最小上下文读取必要证据。

如果 AI 每次都直接读取全仓、全量文档、全量索引或第三方资料，会带来 token 浪费、上下文污染和错误引用风险。因此需要一套本地记忆层，帮助 AI 按需检索、按片段读取、按影响范围更新。

---

## 2. 目标

1. 建立面向 AI 工具开发的本地记忆层。
2. 将公司正式资产、AI 记忆层、项目脚本和 skills 解耦。
3. 支持前端、.NET Core、Java、Python，并预留扩展其它语言。
4. 让语言级 skill 在开发前先按需读取工程规范、使用手册、内部框架能力和服务记忆。
5. 使用脚本完成扫描、生成、校验、查询和受控读取，避免 AI 直接全量解析大文件。
6. 支持本地 FTS 检索，并预留阿里云、腾讯云、火山云、自建向量库等可插拔检索后端。
7. 让人工维护的公司文档保持人类可读，不要求为了 AI 手动分片或重写结构。
8. 在 AI 修改代码或文档后，按影响范围更新或校验记忆。

---

## 3. 非目标

1. 不把 `.tw-memory` 作为公司正式文档资产目录。
2. 不把使用手册正文、工程规范正文或源码副本复制进 `.tw-memory`。
3. 不把第三方官方文档、第三方源码阅读结果或第三方框架使用知识沉淀为长期通用记忆。
4. 不在第一阶段建设 MCP 作为默认记忆读取层。
5. 不要求人工为了 AI 手工维护 chunk、行号、分片或标题层级。
6. 不让 AI 直接打开大索引文件做全量解析。
7. 不提交 SQLite、DB、embedding vectors、FTS 运行库或大段网页抓取内容。

第三方框架研究放在具体设计文档阶段处理。某个功能设计需要研究第三方框架时，AI 可临时查官方文档或源码，并把结论写入该设计文档或 ADR；这些内容不进入通用长期记忆层。

---

## 4. 总体边界

### 4.1 公司正式资产

`docs/` 和源码目录属于公司正式资产或代码事实来源。

适合放在 `docs/` 或源码包 README 的内容：

1. 工程规范。
2. 架构文档。
3. 设计文档。
4. 决策记录。
5. 框架公共方法中文使用手册。
6. 微服务开发说明。

AI 修改或新增框架通用方法、公共组件、微服务模板或重要开发约定时，必须更新对应简体中文使用手册。手册正文属于公司内部资产，不属于 `.tw-memory`。

### 4.2 AI 记忆层

`.tw-memory/` 是 AI 工具记忆层。它只保存：

1. 来源索引。
2. 结构关系。
3. 路由索引。
4. chunk 元数据。
5. 摘要、关键词、关系、hash 和证据指针。
6. 检索后端同步材料。

`.tw-memory` 不保存手册正文，不保存规范正文，不保存源码副本。

### 4.3 记忆引擎

`tools/tw-memory/` 是项目级记忆引擎。它负责：

1. 扫描事实来源。
2. 生成 `.tw-memory`。
3. 校验索引新鲜度和污染风险。
4. 查询记忆。
5. 按 chunk 读取事实片段。
6. 构建本地 FTS。
7. 同步可插拔向量后端。

脚本不放进 skill 内。Skill 只调用脚本。

### 4.4 Thin Skills

`.agents/skills/tw-*` 是轻量技能入口。所有 skill 使用英文编写，名称使用 `tw-` 前缀。

Skill 负责：

1. 触发条件。
2. AI 语义分析要求。
3. 调用 `tools/tw-memory` 的流程。
4. 开发前必须读取哪些记忆。
5. 开发后如何运行 postflight。

Skill 不承载完整记忆层，不内置项目级脚本。

### 4.5 检索后端

SQLite FTS5、向量库、云知识库和 MCP 都不能成为事实来源。

事实来源来自 `docs`、源码、README、包管理文件和 `.tw-memory` 三层索引。检索后端只负责更快召回候选。

---

## 5. `.tw-memory` 三层结构

推荐目录：

```text
.tw-memory/
  README.md
  taxonomy.yaml

  source-index/
    docs.generated.json
    code.generated.json
    packages.generated.json

  graph/
    languages/
    frameworks/
    services/
    capabilities/

  route-index/
    index.generated.json
    by-language/
    by-kind/
    by-service/
    by-framework/

  generated/
    chunks/
    fts/
    vector/

  adapters/
    vector-backends.yaml
```

### 5.1 第一层：来源索引

`source-index/` 指向事实来源，不复制正文。

记录内容：

1. `source_path`
2. `source_hash`
3. 来源类型：规范、手册、README、源码、包管理文件、服务目录。
4. 所属语言。
5. 所属框架或服务。
6. 生成器版本。

### 5.2 第二层：结构记忆

`graph/` 使用 YAML 记录结构关系。

记录内容：

1. 语言栈。
2. 内部二次封装框架。
3. 微服务。
4. 内部能力。
5. 包依赖事实。
6. 使用手册路径。
7. 证据路径。

`graph/` 可以记录内部框架依赖了某个第三方包，但不记录第三方包本身怎么使用。

### 5.3 第三层：路由索引

`route-index/` 使用 JSON，是低 token 入口。

`route-index/index.generated.json` 必须保持极薄，只列分片，不保存全量候选、chunk 全量摘要或正文。

示例：

```json
{
  "schema_version": "1.0",
  "generated_at": "2026-04-30T00:00:00+08:00",
  "repo_hash": "<git-tree-hash>",
  "shards": [
    {
      "id": "dotnet",
      "kind": "language",
      "path": ".tw-memory/route-index/by-language/dotnet.generated.json",
      "summary": ".NET Core 语言记忆入口"
    }
  ]
}
```

AI 不应直接打开分片做全量解析。正常读取必须通过 `tools/tw-memory` 查询。

### 5.4 支撑目录

`generated/` 是可重建生成物目录，不是权威三层。

`adapters/` 保存检索后端配置，不保存事实。

---

## 6. 使用手册处理

使用手册属于公司内部资产，不属于 `.tw-memory`。

规则：

1. 跨框架、跨团队的手册放 `docs/manuals/` 或项目约定的公司文档区。
2. 单个框架包自己的手册优先放对应源码包 `README.md` 或源码包内 `docs/`。
3. 微服务开发手册放服务所属目录的 `README.md` 或服务仓库文档区。
4. 手册正文必须使用简体中文。
5. `.tw-memory` 只记录手册路径、hash、摘要、chunk、关联能力和适用语言。
6. AI 修改框架 public API 或通用方法后，必须同步更新中文使用手册。
7. 如果框架 public API 变化但相关手册未变化，`check` 应报诊断。
8. 人工修改手册后，不要求人工维护 chunk；运行 `generate/check` 后脚本自动更新索引。

---

## 7. `tools/tw-memory` 命令

主入口：

```powershell
python tools\tw-memory\tw_memory.py <command>
```

### 7.1 preflight

开发任务开始前运行。

```powershell
python tools\tw-memory\tw_memory.py preflight `
  --task "<AI semantic query>" `
  --stack dotnet `
  --path "<target path>" `
  --format brief
```

`preflight` 只读检查，不写文件。

职责：

1. 检查 `.tw-memory` 是否存在。
2. 检查索引是否与当前 Git tree 匹配。
3. 检查本地 FTS 或检索缓存是否缺失或陈旧。
4. 基于 AI 语义分析后的查询返回相关规范、手册、内部框架能力和服务记忆。
5. 返回需要执行的后续动作，例如 `generate/check` 或 `build-search`。

`preflight` 不自动修改仓库，不自动联网，不自动摄取第三方资料。

### 7.2 scan

```powershell
python tools\tw-memory\tw_memory.py scan
```

扫描 `docs`、源码、README、包管理文件、语言目录、服务目录和内部框架目录，输出诊断和候选来源。

### 7.3 generate

```powershell
python tools\tw-memory\tw_memory.py generate
```

生成或更新 `.tw-memory` 的来源索引、结构记忆、路由索引和 chunk 元数据。

### 7.4 check

```powershell
python tools\tw-memory\tw_memory.py check
```

校验：

1. 索引是否陈旧。
2. source hash 是否匹配。
3. chunk 行号是否有效。
4. 手册是否可能未同步。
5. 路径是否断链。
6. `.tw-memory` 是否误提交大文件、DB、向量缓存或第三方原文。
7. 根路由索引是否过大。

### 7.5 query

```powershell
python tools\tw-memory\tw_memory.py query `
  --text "缓存封装 cache distributed cache redis tw.caching" `
  --stack dotnet `
  --kind framework `
  --format brief `
  --limit 5
```

`query` 由脚本读取索引、FTS 或可插拔后端，只返回小结果给 AI。

### 7.6 read

```powershell
python tools\tw-memory\tw_memory.py read `
  --chunk-id "dotnet.framework.cache.usage#chunk-001" `
  --format evidence
```

读取规则：

1. 默认只读命中 chunk。
2. 需要上下文时显式使用 `--with-neighbors 1`。
3. 少数情况下可使用 `--format full-section`。
4. 不做硬 token 预算裁剪。
5. 默认保持最小片段。
6. 读取前必须校验 `source_hash`。
7. 如果源文件变化导致 chunk 陈旧，返回 stale，提示运行 `generate/check`，不能继续按旧行号读取。

### 7.7 postflight

AI 修改完成后运行。

```powershell
python tools\tw-memory\tw_memory.py postflight `
  --changed-files "<file-list>" `
  --format brief
```

`postflight` 判断变更是否影响记忆：

1. 是否修改 `docs` 手册或规范。
2. 是否修改源码 README。
3. 是否修改内部框架 public API。
4. 是否修改包管理文件。
5. 是否修改服务目录结构。
6. 是否修改 `.tw-memory` taxonomy 或 graph。

如果需要更新，AI 在说明原因后运行 `generate/check`。普通业务代码变更不应无条件重建记忆。

### 7.8 build-search

```powershell
python tools\tw-memory\tw_memory.py build-search --backend fts
```

从 `.tw-memory` 和公司资产生成本地 FTS 缓存。该缓存不提交 Git。

### 7.9 sync-vector

```powershell
python tools\tw-memory\tw_memory.py sync-vector --backend aliyun
python tools\tw-memory\tw_memory.py sync-vector --backend tencent
python tools\tw-memory\tw_memory.py sync-vector --backend volcengine
python tools\tw-memory\tw_memory.py sync-vector --backend self-hosted
```

后期可选。只同步内部记忆相关 chunk，不同步第三方官方文档或第三方源码。

---

## 8. AI 语义分析与检索流程

检索前必须先由 AI 做语义分析。

完整读取线：

```text
用户需求
  ↓
Skill 触发
  ↓
AI 语义分析
  ↓
生成检索计划
  ↓
tw_memory.py preflight/query
  ↓
脚本查 route-index / graph / FTS / vector
  ↓
返回候选摘要
  ↓
AI 判断候选相关性
  ↓
tw_memory.py read
  ↓
读取事实文件片段
```

AI 语义分析负责：

1. 判断语言栈。
2. 判断任务类型：框架开发、微服务开发、测试、规范查询、手册查询。
3. 提取能力词。
4. 提取框架、服务、包名和技术词。
5. 结合当前路径判断影响范围。
6. 生成同义词和查询扩展。
7. 决定先查哪个范围。

脚本负责：

1. 召回。
2. 去重。
3. 排序。
4. hash 校验。
5. stale 检测。
6. 返回小结果。

---

## 9. 语义增强分级

语义增强在 `generate` 阶段完成，而不是写入 DB 前临时完成。

生成线：

```text
docs / README / 源码 / 包管理文件
  ↓
scan
  ↓
chunk
  ↓
语义增强
  ↓
写入 .tw-memory 三层记忆
  ↓
生成 FTS / vector / 云知识库索引
```

语义增强是检索元数据，不替代事实来源。

分级：

1. **L0 元数据**
   路径、hash、类型、语言、生成器版本。脚本生成，所有来源都有。

2. **L1 轻摘要**
   标题、短 summary、关键词。重要文档和 chunk 有。

3. **L2 关系增强**
   能力、框架、服务、依赖关系。只给核心内部框架、服务和规范。

4. **L3 深度增强**
   高价值内部资产才做，例如复杂内部框架手册、核心公共能力。第三方原始资料不做长期深度增强。

不允许对所有内容无差别重度语义增强。默认只做 L0/L1，按价值进入 L2/L3。

---

## 10. 人工文档与自动 chunk

公司内部资产保持人类可读，不为 AI 强制改结构。

规则：

1. 人工可以正常维护 `docs` 和 README。
2. 不要求人工拆文件。
3. 不要求人工维护 chunk。
4. 不要求标题层级完全规范。
5. 脚本优先按 Markdown 标题、列表、代码块边界生成自然 chunk。
6. 标题不规范时，按段落、行数或 token 近似窗口生成 synthetic chunk。
7. synthetic chunk 保留少量 overlap。
8. 每个 chunk 记录 `chunk_id`、`source_path`、`source_hash`、`start_line`、`end_line`、`summary`、`keywords`、`relations`。

AI 读取时只能通过 `query/read` 读取命中片段。

---

## 11. 第三方框架范围收缩

第三方框架原始知识不进入长期记忆层。

不建设以下长期记忆能力：

1. 第三方官方文档长期记忆。
2. 第三方源码长期记忆。
3. 第三方文档摄取记录。
4. 第三方 trust level 体系。
5. `intake third-party` 命令。

`.tw-memory` 可以记录内部框架依赖某第三方包的事实，例如包名、版本、lockfile hash 和证据路径。但不记录第三方包怎么用。

如果具体设计需要研究第三方框架，研究发生在该设计文档阶段，结论写入对应设计文档或 ADR。

AI 开发时优先查询内部二次封装能力和公司使用手册。内部封装无法满足需求时，才在设计阶段讨论是否直接使用第三方能力。

---

## 12. 可插拔检索后端

第一阶段默认使用 SQLite FTS5。

后期可选：

1. 阿里云知识库。
2. 腾讯云知识库或向量检索。
3. 火山云知识库或向量服务。
4. 自建向量库。

统一约束：

1. 后端只索引内部记忆、公司规范索引、公司手册索引、内部二次封装能力、微服务结构、语言级目录总览。
2. 后端不索引第三方官方文档和第三方源码。
3. 后端只保存由 `.tw-memory` 和公司资产生成的 chunk、embedding 和 metadata。
4. 命中后必须通过 `tw_memory.py read` 回读事实文件片段。
5. 后端不能成为事实来源。
6. 后端同步失败不影响本地三层记忆，只影响语义检索增强。

MCP 如后期存在，只能薄封装 `tools/tw-memory`，不能绕开脚本读取记忆，也不能拥有独立检索逻辑。

---

## 13. Skill 触发规则

初始 skills：

```text
.agents/skills/tw-frontend/SKILL.md
.agents/skills/tw-dotnet/SKILL.md
.agents/skills/tw-java/SKILL.md
.agents/skills/tw-python/SKILL.md
.agents/skills/tw-memory/SKILL.md
```

触发规则：

1. 用户显式指定时触发。
2. AI 根据任务路径和语言自动触发。
3. 每个开发任务开始前触发一次主 skill。
4. 编码过程中不因每次写文件反复触发。
5. 任务范围变化时可扩展触发。
6. 跨语言、跨前后端、跨服务时触发相关辅助 skill。
7. 同语言多服务不切 skill，只读取多个服务记忆。
8. 触发新 skill 前必须说明原因。
9. 不允许因为“可能相关”就加载全部语言记忆。

多服务协调：

1. 同语言多服务：继续当前 skill，读取多个服务结构记忆。
2. 跨语言多服务：触发对应语言 skill。
3. 跨前后端：触发 frontend 和 backend 对应 skill。
4. 涉及契约：查询契约记忆，后期可增加 `tw-contracts`。

`tw-memory` skill 只用于维护记忆体系本身，不用于普通业务开发。

---

## 14. 工程规范记忆

`docs/standards` 是公司正式规范资产，不属于 `.tw-memory`。

规则：

1. `docs-old/standards` 中的公司规范资产迁移到 `docs/standards` 后，`docs-old` 不再作为文档根目录或索引来源。
2. `docs/standards` 只保留面向正式使用人员的规则正文、流程、决策记录和参考资料。
3. 旧标准体系自带的 front matter 元数据、隐藏 anchor、`meta.schema.json`、`index.generated.json`、`_index/**`、`_meta/**` 和 AI 检索契约不属于正式规范资产，迁移后删除。
4. `.tw-memory` 负责生成规范相关的源索引、chunk 元数据和证据指针，不要求 `docs/standards` 自带独立索引脚本。
5. 语言 skill 必须先按需读取规范。
6. 人工修改规范后运行 `python tools/tw-memory/tw_memory.py generate --format brief` 和 `python tools/tw-memory/tw_memory.py check --format brief` 更新 `.tw-memory`。
7. AI 修改规范正文时写入 `docs/standards`，再运行上述 `generate/check`。

`docs` 内 Markdown 文件应保持人可读跳转；`.tw-memory` 额外记录 `path + line_range + heading/anchor`，供 AI 精确读取。

---

## 15. 目录总览

目录总览分两类。

### 15.1 公司正式目录说明

面向团队阅读的目录说明放公司资产区：

```text
docs/architecture/
docs/manuals/
各源码包 README.md
```

### 15.2 AI 目录能力总览

由脚本生成到 `.tw-memory`：

```text
.tw-memory/source-index/
  repo.generated.json
  languages.generated.json
  frameworks.generated.json
  services.generated.json

.tw-memory/graph/languages/
  frontend.yaml
  dotnet.yaml
  java.yaml
  python.yaml

.tw-memory/graph/frameworks/
  dotnet.tw-core.yaml
  dotnet.tw-caching.yaml

.tw-memory/graph/services/
  auth-service.yaml
  notice-service.yaml
```

总览内容：

1. 工程顶层目录职责。
2. 语言根目录。
3. 包管理文件。
4. 构建和测试入口。
5. 内部二次封装框架列表。
6. 框架能力摘要。
7. 使用手册路径。
8. 服务列表、职责、技术栈、契约入口和依赖服务。

---

## 16. 变更触发策略

### 16.1 拉取代码后

`git pull` 后不后台自动生成或修改文件。

第一次 AI 开发任务开始前，skill 运行 `preflight`。如果发现缓存缺失或索引陈旧，返回明确动作。

### 16.2 变更前

每个 AI 开发任务开始前运行一次 `preflight`。

它只读检查，不写文件。

### 16.3 变更中

不会每次写文件前触发。

仅在任务范围明显变化时重新 preflight，例如：

1. 单语言变多语言。
2. 后端任务变成前后端协同。
3. 新增服务影响。
4. read 发现跨服务影响。
5. 用户中途改变需求范围。

### 16.4 变更后

任务结束前运行 `postflight`。

如果影响记忆，AI 说明原因并运行：

```powershell
python tools\tw-memory\tw_memory.py generate
python tools\tw-memory\tw_memory.py check
```

### 16.5 CI

CI 运行：

```powershell
python tools\tw-memory\tw_memory.py check
```

CI 不自动写文件。发现陈旧则失败并提示本地运行 `generate`。

---

## 17. Git 存储策略

建议提交：

```text
.tw-memory/README.md
.tw-memory/taxonomy.yaml
.tw-memory/source-index/*.generated.json
.tw-memory/graph/**/*.yaml
.tw-memory/route-index/**/*.generated.json
.tw-memory/generated/chunks/**/*.generated.json
```

不提交：

```text
.tw-memory/generated/fts/
.tw-memory/generated/vector/
*.sqlite
*.db
embedding vectors
第三方官方文档全文副本
第三方源码副本
大段抓取网页
FTS5 运行库
向量缓存
```

`check` 文件大小规则：

1. 可提交 `.tw-memory` 单文件超过 `200KB`：warning。
2. 超过 `1MB`：error。
3. 发现 `.sqlite`、`.db`、embedding vectors、大段抓取网页、第三方源码副本：error。

---

## 18. 成功标准

1. AI 开发任务开始前能运行 `preflight`。
2. `preflight` 返回最相关的规范、手册、内部框架能力和服务记忆。
3. AI 不需要直接读取大索引文件。
4. AI 能通过 `query/read` 读取精确 chunk。
5. 框架 public API 变化后能提示或要求更新中文使用手册。
6. 人工修改 `docs` 或 README 后，`generate/check` 能更新 `.tw-memory`。
7. CI 能发现陈旧索引、断链、hash 不一致和误提交大文件。
8. 多语言、多服务任务能按范围触发相关 skill，不全量加载。
9. FTS、向量库、云知识库和 MCP 都只做检索后端或薄入口，不作为事实来源。
10. 第三方框架原始知识不进入长期记忆层。

---

## 19. 风险约束

1. `.tw-memory` 不保存密钥、token、连接串。
2. `.tw-memory` 不保存聊天记录。
3. `.tw-memory` 不保存未验证猜测。
4. `.tw-memory` 不保存第三方官方文档全文或源码副本。
5. DB、FTS、embedding、向量缓存不提交 Git。
6. 根路由索引必须保持极薄，不可无限增长。
7. 大索引和事实文件不得由 AI 直接全量解析。
8. 所有读取通过 `tools/tw-memory`。
9. MCP 只能薄封装脚本，不拥有独立检索逻辑。
10. 使用手册正文属于公司资产，不放 `.tw-memory`。
11. 公司文档不要求为 AI 手工分片。

---

## 20. 第一阶段范围

第一阶段实现：

1. `.tw-memory` 骨架。
2. `tools/tw-memory` CLI。
3. 本地 SQLite FTS5。
4. 自动 chunk。
5. `preflight` / `postflight`。
6. `query` / `read`。
7. 语言级 thin skills。
8. 内部二次封装能力记忆。
9. 工程规范索引。
10. 使用手册索引。

后期增强：

1. 阿里云、腾讯云、火山云或自建向量后端。
2. MCP 薄封装。
3. 更复杂的跨仓库协调。
4. 契约专用 skill。

---

## 21. 已收敛的关键问题

1. 放弃 `docs/knowledge` 作为 AI 记忆根目录，改为 `.tw-memory`。
2. 使用手册属于公司内部资产，正文不进入 `.tw-memory`。
3. 脚本放 `tools/tw-memory`，不放 skill 内。
4. Skill 是任务级触发，不是文件级触发。
5. AI 检索前先做语义分析。
6. 脚本负责高效召回和受控读取。
7. `preflight` 只读检查，不写文件。
8. `postflight` 按影响范围决定是否运行 `generate/check`。
9. 第三方框架原始知识不进入长期记忆层。
10. 可插拔知识库只做内部记忆检索增强，不做事实来源。
