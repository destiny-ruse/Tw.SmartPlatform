from __future__ import annotations

import argparse
import unicodedata
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path


SKIP_DIRS = {
    ".git",
    ".idea",
    ".vscode",
    ".vs",
    "bin",
    "build",
    "coverage",
    "dist",
    "node_modules",
    "obj",
}


@dataclass
class DirectoryDoc:
    title: str
    description: str


@dataclass
class Node:
    name: str
    path: Path
    title: str | None = None
    children: dict[str, "Node"] = field(default_factory=dict)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="根据 README.md 生成目录结构说明文档。")
    parser.add_argument("--root", default=".", help="扫描根目录，默认是当前工作目录。")
    parser.add_argument(
        "--output",
        default=None,
        help="输出 Markdown 文件路径，默认是 docs/architecture/directory-structure.md。",
    )
    parser.add_argument("--title", default="目录结构说明", help="输出文档标题。")
    return parser.parse_args()


def should_skip(path: Path) -> bool:
    return any(part in SKIP_DIRS for part in path.parts)


def read_lines(readme: Path) -> list[str]:
    try:
        return readme.read_text(encoding="utf-8").splitlines()
    except UnicodeDecodeError:
        return readme.read_text(encoding="utf-8-sig").splitlines()


def normalize_text(text: str) -> str:
    return text.strip().rstrip("。.;；")


def extract_title(lines: list[str], directory: Path) -> str:
    for line in lines:
        text = line.strip()
        if text.startswith("# ") and not text.startswith("## "):
            return normalize_text(text[2:])
    return normalize_text(directory.name or ".")


def extract_description(lines: list[str]) -> str:
    for line in lines:
        text = line.strip()
        if (
            not text
            or text.startswith("#")
            or text.startswith("```")
            or text.startswith(">")
        ):
            continue
        return text
    return "该目录的描述尚未补充。"


def read_directory_doc(readme: Path) -> DirectoryDoc:
    lines = read_lines(readme)
    return DirectoryDoc(
        title=extract_title(lines, readme.parent),
        description=extract_description(lines),
    )


def collect_readmes(root: Path) -> dict[Path, DirectoryDoc]:
    readmes: dict[Path, DirectoryDoc] = {}
    candidates = [root / "README.md"]
    candidates.extend(root.rglob("README.md"))

    for readme in candidates:
        if not readme.exists() or should_skip(readme.relative_to(root)):
            continue
        readmes[readme.parent] = read_directory_doc(readme)

    return dict(sorted(readmes.items(), key=lambda item: item[0].relative_to(root).parts))


def build_tree(root: Path, docs: dict[Path, DirectoryDoc]) -> Node:
    root_doc = docs.get(root)
    root_node = Node(
        name=root.name or ".",
        path=root,
        title=root_doc.title if root_doc else None,
    )

    for directory, doc in docs.items():
        if directory == root:
            continue

        current = root_node
        current_path = root
        for part in directory.relative_to(root).parts:
            current_path = current_path / part
            current = current.children.setdefault(part, Node(name=part, path=current_path))
        current.title = doc.title

    return root_node


def display_path(path: Path, root: Path) -> str:
    if path == root:
        return "."
    return path.relative_to(root).as_posix()


def display_workspace_path(path: Path, workspace: Path) -> str:
    try:
        relative = path.relative_to(workspace)
    except ValueError:
        return path.as_posix()
    text = relative.as_posix()
    return text if text else "."


def text_width(text: str) -> int:
    width = 0
    for char in text:
        width += 2 if unicodedata.east_asian_width(char) in {"F", "W"} else 1
    return width


def node_title(node: Node) -> str:
    return node.title or ""


def collect_tree_rows(node: Node) -> list[tuple[str, str]]:
    rows = [(f"{node.name}/", node_title(node))]

    def walk(current: Node, prefix: str) -> None:
        children = sorted(current.children.values(), key=lambda child: child.name.lower())
        for index, child in enumerate(children):
            is_last = index == len(children) - 1
            connector = "└─ " if is_last else "├─ "
            rows.append((f"{prefix}{connector}{child.name}/", node_title(child)))
            next_prefix = prefix + ("   " if is_last else "│  ")
            walk(child, next_prefix)

    walk(node, "")
    return rows


def render_tree(node: Node) -> list[str]:
    rows = collect_tree_rows(node)
    rows_with_title = [text for text, title in rows if title]
    comment_column = max(text_width(text) for text in rows_with_title) + 2
    lines = []
    for text, title in rows:
        if not title:
            lines.append(text)
            continue
        padding = " " * max(2, comment_column - text_width(text))
        lines.append(f"{text}{padding}# {title}")
    return lines


def render_table(docs: dict[Path, DirectoryDoc], root: Path) -> list[str]:
    lines = [
        "| 目录 | 描述 |",
        "| --- | --- |",
    ]
    for directory, doc in docs.items():
        lines.append(f"| `{display_path(directory, root)}` | {doc.description} |")
    return lines


def render_document(
    title: str,
    root: Path,
    output: Path,
    docs: dict[Path, DirectoryDoc],
    workspace: Path,
) -> str:
    tree = build_tree(root, docs)
    generated_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    root_display = display_workspace_path(root, workspace)
    output_display = display_workspace_path(output, workspace)

    lines = [
        f"# {title}",
        "",
        f"本文档从 `{root_display}` 开始扫描，根据各级 `README.md` 的一级标题生成目录树说明，并使用正文描述生成目录说明表。",
        "",
        f"- 生成时间：{generated_at}",
        f"- README 数量：{len(docs)}",
        f"- 输出文件：`{output_display}`",
        "",
        "## 目录树",
        "",
        "```text",
        *render_tree(tree),
        "```",
        "",
        "## 目录说明",
        "",
        *render_table(docs, root),
        "",
    ]
    return "\n".join(lines)


def main() -> None:
    args = parse_args()
    workspace = Path.cwd().resolve()
    root = Path(args.root)
    if not root.is_absolute():
        root = workspace / root
    root = root.resolve()

    output = Path(args.output) if args.output else workspace / "docs" / "architecture" / "directory-structure.md"
    if not output.is_absolute():
        output = workspace / output
    output = output.resolve()

    if not root.exists() or not root.is_dir():
        raise SystemExit(f"扫描根目录不存在或不是目录：{root}")

    docs = collect_readmes(root)
    if not docs:
        raise SystemExit(f"扫描根目录内未找到 README.md：{root}")

    output.parent.mkdir(parents=True, exist_ok=True)
    document = render_document(args.title, root, output, docs, workspace)
    output.write_text(document, encoding="utf-8")

    print(f"已生成：{output}")
    print(f"扫描根目录：{root}")
    print(f"README 数量：{len(docs)}")


if __name__ == "__main__":
    main()
