from __future__ import annotations

import re
from collections.abc import Sequence
from pathlib import Path

from .hashing import file_sha256
from .models import ChunkRecord


HEADING_RE = re.compile(r"^(#{1,6})[ \t]+(.+?)[ \t#]*$")
FENCE_RE = re.compile(r"^[ \t]{0,3}(`{3,}|~{3,})")
WINDOW_LINES = 80
OVERLAP_LINES = 8


class MarkdownChunker:
    def __init__(self, path: Path, base_id: str, lines: Sequence[str] | None = None):
        self.path = path
        self.base_id = base_id
        self.lines = list(lines) if lines is not None else None

    def chunk(self) -> list[ChunkRecord]:
        lines = self.lines if self.lines is not None else self.path.read_text(encoding="utf-8").splitlines()
        if not lines:
            return []

        source_hash = file_sha256(self.path)
        heading_starts = self._heading_starts(lines)
        if not heading_starts:
            return self._synthetic_chunks(lines, source_hash)

        chunks: list[ChunkRecord] = []
        for index, (start_line, heading) in enumerate(heading_starts):
            next_start = heading_starts[index + 1][0] if index + 1 < len(heading_starts) else len(lines) + 1
            chunks.extend(
                self._windowed_records(
                    lines=lines,
                    number_offset=len(chunks),
                    source_hash=source_hash,
                    start_line=start_line,
                    end_line=next_start - 1,
                    heading=heading,
                )
            )
        return chunks

    def _heading_starts(self, lines: list[str]) -> list[tuple[int, str]]:
        starts: list[tuple[int, str]] = []
        in_fence = False
        fence_marker = ""

        for line_number, line in enumerate(lines, start=1):
            fence_match = FENCE_RE.match(line)
            if fence_match:
                marker = fence_match.group(1)
                marker_char = marker[0]
                if not in_fence:
                    in_fence = True
                    fence_marker = marker_char
                elif marker_char == fence_marker:
                    in_fence = False
                    fence_marker = ""
                continue

            if in_fence:
                continue

            heading_match = HEADING_RE.match(line)
            if heading_match:
                starts.append((line_number, heading_match.group(2).strip()))

        return starts

    def _synthetic_chunks(self, lines: list[str], source_hash: str) -> list[ChunkRecord]:
        return self._windowed_records(
            lines=lines,
            number_offset=0,
            source_hash=source_hash,
            start_line=1,
            end_line=len(lines),
            heading=None,
        )

    def _windowed_records(
        self,
        *,
        lines: list[str],
        number_offset: int,
        source_hash: str,
        start_line: int,
        end_line: int,
        heading: str | None,
    ) -> list[ChunkRecord]:
        chunks: list[ChunkRecord] = []
        if end_line < start_line:
            return chunks

        current_start = start_line
        while current_start <= end_line:
            current_end = self._window_end_preserving_fence(
                lines=lines,
                section_start=start_line,
                nominal_end=min(current_start + WINDOW_LINES - 1, end_line),
                end_line=end_line,
            )
            chunks.append(
                self._record(
                    number=number_offset + len(chunks) + 1,
                    source_hash=source_hash,
                    start_line=current_start,
                    end_line=current_end,
                    heading=heading,
                )
            )
            if current_end == end_line:
                break
            current_start = max(start_line, current_end - OVERLAP_LINES + 1)
        return chunks

    def _window_end_preserving_fence(
        self,
        *,
        lines: list[str],
        section_start: int,
        nominal_end: int,
        end_line: int,
    ) -> int:
        if nominal_end >= end_line:
            return nominal_end

        in_fence = False
        fence_marker = ""
        for line in lines[section_start - 1 : nominal_end]:
            fence_match = FENCE_RE.match(line)
            if not fence_match:
                continue

            marker_char = fence_match.group(1)[0]
            if not in_fence:
                in_fence = True
                fence_marker = marker_char
            elif marker_char == fence_marker:
                in_fence = False
                fence_marker = ""

        if not in_fence:
            return nominal_end

        for line_number in range(nominal_end + 1, end_line + 1):
            fence_match = FENCE_RE.match(lines[line_number - 1])
            if fence_match and fence_match.group(1)[0] == fence_marker:
                return line_number

        return end_line

    def _record(
        self,
        *,
        number: int,
        source_hash: str,
        start_line: int,
        end_line: int,
        heading: str | None,
    ) -> ChunkRecord:
        return ChunkRecord(
            chunk_id=f"{self.base_id}#chunk-{number:03d}",
            source_path=self.path.as_posix(),
            source_hash=source_hash,
            start_line=start_line,
            end_line=end_line,
            heading=heading,
            summary="",
            keywords=[],
            relations={},
        )
