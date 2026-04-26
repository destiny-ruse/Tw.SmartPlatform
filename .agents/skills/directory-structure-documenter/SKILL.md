---
name: directory-structure-documenter
description: 当用户需要创建或更新项目目录结构文档、解释各目录作用、根据 README.md 的一级标题和正文描述汇总目录职责时使用此技能。支持用户用自然语言指定扫描根目录、输出目录和文件名；默认从当前项目根目录开始扫描，并输出到 docs/architecture/directory-structure.md。
---

# 目录结构文档生成技能

这个技能用于快速创建或更新“目录结构说明”类文档。它读取各目录 `README.md` 的一级标题作为目录树解释，并读取正文描述作为目录说明表内容，帮助维护者快速理解项目边界。

## 适用场景

- 用户要求“生成目录结构文档”“更新目录说明”“解释每个目录作用”。
- 用户要求基于 README 的标题和描述汇总项目结构。
- 用户用自然语言指定根目录、输出路径或文件名，例如“从 backend/dotnet 开始生成到 docs/architecture/dotnet-structure.md”。

## 默认约定

- 默认扫描根目录：当前工作区根目录。
- 默认输出目录：`docs/architecture`。
- 默认输出文件名：`directory-structure.md`。
- 默认语言：中文。
- 默认跳过：`.git`、`node_modules`、`.vs`、`bin`、`obj`、`dist`、`build`、`coverage` 等生成目录或缓存目录。

## 扫描规则

1. 从用户指定的根目录开始；如果用户没有指定，就从当前项目根目录开始。
2. 查找根目录及其下级目录中的 `README.md`。
3. 沿着仍能发现 `README.md` 的下级路径继续扫描；当某个分支后续不再存在 `README.md` 时，该分支停止展开。
4. 使用每个 `README.md` 的一级标题作为目录树中的目录解释。
5. 使用每个 `README.md` 的第一句正文描述作为目录说明表中的描述。
6. 目录树中的 `#` 注释需要按列对齐，方便阅读。
7. 如果树状结构中必须展示某个中间目录，但该目录没有 `README.md`，只保留目录结构，不填写解释。

## 工作流程

1. 解析用户自然语言中的三个信息：扫描根目录、输出目录、输出文件名。
2. 对缺失的信息套用默认约定。
3. 先确认目标路径不会覆盖用户明确不想覆盖的文件；如果是更新既有目录结构文档，可以直接覆盖。
4. 优先运行 `scripts/generate_directory_structure_doc.py` 生成文档。
5. 生成后快速检查文档是否包含标题、扫描范围、目录树和目录说明表。
6. 检查目录树的 `#` 注释是否对齐，目录说明表是否使用 README 正文描述。
7. 向用户汇报输出文件路径，以及扫描到的 README 数量。

## 脚本用法

```powershell
python .agents/skills/directory-structure-documenter/scripts/generate_directory_structure_doc.py
```

指定根目录和输出文件：

```powershell
python .agents/skills/directory-structure-documenter/scripts/generate_directory_structure_doc.py --root backend/dotnet --output docs/architecture/dotnet-directory-structure.md
```

## 输出结构

生成的 Markdown 文档使用以下结构：

```markdown
# 目录结构说明

本文档说明扫描根目录、生成时间和 README 扫描规则。

## 目录树

以树状文本展示目录层级，并在每个目录后使用一级标题标注作用；`#` 注释需要按列对齐。

## 目录说明

用表格列出每个包含 README.md 的目录及其正文描述。
```

## 写作要求

- 保持中文、简洁、可维护。
- 目录树使用 README 一级标题，不依赖额外字段。
- 目录说明表使用 README 正文描述，保留完整含义。
- 如果 README 缺少一级标题，使用目录名兜底。
- 如果 README 缺少正文描述，使用“该目录的描述尚未补充。”提醒维护者补齐源文档。
- 不把生成目录、缓存目录或依赖目录写入架构文档。
