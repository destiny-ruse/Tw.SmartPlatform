"""在隔离 Python 进程中加载仓库内 tw_memory 命令行入口"""

from __future__ import annotations

import sys
from pathlib import Path


def main(argv: list[str] | None = None) -> int:
    """从固定仓库布局加载 tw_memory 并转发命令参数

    Args:
        argv: 不包含 Python 可执行文件和脚本路径的命令参数

    Returns:
        tw_memory 命令行入口返回的进程退出码
    """
    tools_root = Path(__file__).resolve().parents[1]
    source_root = tools_root / "src"
    sys.path.insert(0, str(source_root))

    from tw_memory.cli import main as tw_memory_main

    return tw_memory_main(argv)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
