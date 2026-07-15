from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

import yaml


REPO_ROOT = Path(__file__).resolve().parents[2]


def test_tw_memory_hook_uses_isolated_repository_runner() -> None:
    """验证 hook 仅安装外部依赖并通过仓库脚本加载工具源码"""
    config = yaml.safe_load((REPO_ROOT / ".pre-commit-config.yaml").read_text(encoding="utf-8"))
    hook = config["repos"][0]["hooks"][0]

    assert hook["entry"] == "python -I tools/scripts/run_tw_memory.py check --staged"
    assert hook["language"] == "python"
    assert hook["additional_dependencies"] == ["PyYAML>=6.0"]
    assert hook["pass_filenames"] is False
    assert hook["always_run"] is True


def test_tw_memory_runner_ignores_ambient_pythonpath() -> None:
    """验证隔离入口不依赖调用方的 PYTHONPATH"""
    runner = REPO_ROOT / "tools/scripts/run_tw_memory.py"
    environment = os.environ.copy()
    environment["PYTHONPATH"] = str(REPO_ROOT / "missing-python-path")

    result = subprocess.run(
        [sys.executable, "-I", str(runner), "--help"],
        cwd=REPO_ROOT,
        env=environment,
        capture_output=True,
        text=True,
        check=False,
    )

    assert result.returncode == 0, result.stderr
    assert "tw-memory" in result.stdout
