from __future__ import annotations

import hashlib
import re
from pathlib import Path

from tw_memory.hashing import sha256_normalized

_HEADING = re.compile(r"^(#{1,6})\s+(.+?)\s*$")
_FENCE = re.compile(r"^\s*(```|~~~)")


def _slug(title: str) -> str:
    """Return the segment anchor for a Markdown heading title."""
    return re.sub(r"\s+", "-", title.strip())


def _relative_slash_path(root: Path, path: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


def segment_markdown(root: Path, path: Path) -> list[dict[str, object]]:
    """Split a Markdown document into heading-based content segments."""
    relative_path = _relative_slash_path(root, path)
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    headings: list[tuple[int, int, str]] = []

    in_fence = False
    for line_number, line in enumerate(lines, start=1):
        if _FENCE.match(line):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        match = _HEADING.match(line.rstrip("\n\r"))
        if match:
            headings.append((line_number, len(match.group(1)), match.group(2).strip()))

    if not headings:
        return [
            {
                "segment_id": f"{relative_path}#document",
                "path": relative_path,
                "title": "document",
                "level": 0,
                "start_line": 1,
                "end_line": len(lines),
                "sha256": sha256_normalized(path),
            }
        ]

    segments: list[dict[str, object]] = []
    for index, (start_line, level, title) in enumerate(headings):
        next_start = headings[index + 1][0] if index + 1 < len(headings) else len(lines) + 1
        end_line = next_start - 1
        segment_text = "".join(lines[start_line - 1 : end_line])
        segments.append(
            {
                "segment_id": f"{relative_path}#{_slug(title)}",
                "path": relative_path,
                "title": title,
                "level": level,
                "start_line": start_line,
                "end_line": end_line,
                "sha256": hashlib.sha256(segment_text.encode("utf-8")).hexdigest(),
            }
        )

    return segments
