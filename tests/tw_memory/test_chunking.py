import tempfile
import unittest
from pathlib import Path

from tools.tw_memory_test_support import import_engine


engine = import_engine()
MarkdownChunker = engine.chunking.MarkdownChunker


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
