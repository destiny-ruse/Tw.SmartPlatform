from __future__ import annotations

from pathlib import Path

from tests.conftest import write_text
from tw_memory.markdown_segments import segment_markdown


def test_segment_markdown_returns_heading_segments(repo: Path) -> None:
    path = write_text(
        repo / "standard.md",
        "# 标准\n\n## 目标\n\nA\n\n## 检查清单\n\n- X\n",
    )

    segments = segment_markdown(repo, path)

    assert [segment["title"] for segment in segments] == ["标准", "目标", "检查清单"]
    assert segments[0]["start_line"] == 1
    assert segments[1]["path"] == "standard.md"
    assert segments[2]["segment_id"] == "standard.md#检查清单"


def test_segment_markdown_ignores_headings_in_fenced_code(repo: Path) -> None:
    path = write_text(
        repo / "standard.md",
        "# 标准\n\n```sh\n# not a heading\n```\n\n## 目标\n",
    )

    segments = segment_markdown(repo, path)

    assert [segment["title"] for segment in segments] == ["标准", "目标"]
