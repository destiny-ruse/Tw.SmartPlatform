from __future__ import annotations

import argparse
import json
from typing import Sequence

from .paths import repo_root
from .scanner import SourceScanner


COMMANDS = (
    "scan",
    "generate",
    "check",
    "query",
    "read",
    "preflight",
    "postflight",
    "build-search",
    "sync-vector",
)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="tw_memory.py")
    subparsers = parser.add_subparsers(dest="command", required=True)

    scan = subparsers.add_parser("scan", help="Scan fact sources without writing memory files.")
    scan.add_argument("--format", choices=("brief", "json"), default="brief")

    generate = subparsers.add_parser("generate", help="Generate .tw-memory artifacts.")
    generate.add_argument("--format", choices=("brief", "json"), default="brief")

    check = subparsers.add_parser("check", help="Validate .tw-memory freshness and safety.")
    check.add_argument("--format", choices=("brief", "json"), default="brief")

    query = subparsers.add_parser("query", help="Query memory candidates.")
    query.add_argument("--text", required=True)
    query.add_argument("--stack")
    query.add_argument("--kind")
    query.add_argument("--format", choices=("brief", "json"), default="brief")
    query.add_argument("--limit", type=int, default=5)

    read = subparsers.add_parser("read", help="Read one checked evidence chunk.")
    read.add_argument("--chunk-id", required=True)
    read.add_argument("--format", choices=("evidence", "json", "full-section"), default="evidence")
    read.add_argument("--with-neighbors", type=int, default=0)

    preflight = subparsers.add_parser("preflight", help="Run read-only task memory preflight.")
    preflight.add_argument("--task", required=True)
    preflight.add_argument("--stack")
    preflight.add_argument("--path")
    preflight.add_argument("--format", choices=("brief", "json"), default="brief")

    postflight = subparsers.add_parser("postflight", help="Classify changed files after AI edits.")
    postflight.add_argument("--changed-files", required=True)
    postflight.add_argument("--format", choices=("brief", "json"), default="brief")

    build_search = subparsers.add_parser("build-search", help="Build local search cache.")
    build_search.add_argument("--backend", choices=("fts",), default="fts")
    build_search.add_argument("--format", choices=("brief", "json"), default="brief")

    sync_vector = subparsers.add_parser("sync-vector", help="Sync allowed internal memory to vector backend.")
    sync_vector.add_argument("--backend", required=True)
    sync_vector.add_argument("--format", choices=("brief", "json"), default="brief")

    return parser


def main(argv: Sequence[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.command == "scan":
        records = SourceScanner(repo_root()).scan()
        payload = {"sources": [record.to_json() for record in records], "writes": []}
        if args.format == "json":
            print(json.dumps(payload, ensure_ascii=False, indent=2))
        else:
            print(f"sources: {len(records)}")
        return 0

    parser.error(f"{args.command} is wired but not implemented in this task")
    return 2
