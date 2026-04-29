import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MarkdownChunker = engine.chunking.MarkdownChunker
WINDOW_LINES = engine.chunking.WINDOW_LINES
OVERLAP_LINES = engine.chunking.OVERLAP_LINES


class ChunkingTests(unittest.TestCase):
    def test_markdown_headings_create_natural_chunks(self):
        text = "# Title\n\nintro\n\n## Usage\n\nrun this\n\n## API\n\ncall that\n"
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertEqual([chunk.heading for chunk in chunks], ["Title", "Usage", "API"])
            self.assertEqual(chunks[0].start_line, 1)
            self.assertEqual(chunks[0].end_line, 4)
            self.assertEqual(chunks[1].start_line, 5)
            self.assertEqual(chunks[1].end_line, 8)

    def test_code_fences_do_not_split_inside_fence(self):
        text = "# Title\n\n```text\n## not heading\n```\n\n## Real\n\nbody\n"
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertEqual([chunk.heading for chunk in chunks], ["Title", "Real"])
            self.assertEqual(chunks[0].end_line, 6)

    def test_large_heading_section_splits_into_overlapping_windows(self):
        text = "# Huge\n" + "\n".join(f"line {index}" for index in range(1, WINDOW_LINES + 30)) + "\n"
        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertGreater(len(chunks), 1)
            self.assertEqual([chunk.chunk_id for chunk in chunks], ["docs.readme#chunk-001", "docs.readme#chunk-002"])
            for chunk in chunks:
                self.assertLessEqual(chunk.end_line - chunk.start_line + 1, WINDOW_LINES)
                self.assertEqual(chunk.heading, "Huge")
            self.assertEqual(chunks[0].start_line, 1)
            self.assertEqual(chunks[0].end_line, WINDOW_LINES)
            self.assertEqual(chunks[1].start_line, WINDOW_LINES - OVERLAP_LINES + 1)

    def test_large_heading_section_window_does_not_end_inside_fence(self):
        lines = ["# Huge"]
        lines.extend(f"before {index}" for index in range(1, WINDOW_LINES - 5))
        lines.append("```python")
        lines.extend(f"print({index})" for index in range(1, 12))
        lines.append("```")
        lines.extend(f"after {index}" for index in range(1, 12))
        text = "\n".join(lines) + "\n"

        with tempfile.TemporaryDirectory() as work:
            path = Path(work) / "README.md"
            path.write_text(text, encoding="utf-8")

            chunks = MarkdownChunker(path, "docs.readme").chunk()

            self.assertGreater(len(chunks), 1)
            self.assertEqual(chunks[0].start_line, 1)
            self.assertGreater(chunks[0].end_line - chunks[0].start_line + 1, WINDOW_LINES)
            self.assertEqual(lines[chunks[0].end_line - 1], "```")
