from __future__ import annotations

import argparse
import json
from typing import Sequence

from .checker import MemoryChecker
from .generator import MemoryGenerator
from .paths import repo_root
from .postflight import PostflightRunner
from .preflight import PreflightRunner
from .reader import ChunkReader
from .scanner import SourceScanner
from .search import SearchIndex
from .vector import VectorSyncRunner


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

    if args.command == "generate":
        result = MemoryGenerator(repo_root()).generate()
        if args.format == "json":
            print(json.dumps(result.to_json(), ensure_ascii=False, indent=2))
        else:
            print(f"generated: {result.generated_count}")
            print(f"diagnostics: {len(result.errors)}")
        return 1 if result.errors else 0

    if args.command == "check":
        diagnostics = MemoryChecker(repo_root()).check()
        if args.format == "json":
            print(json.dumps({"diagnostics": [item.to_json() for item in diagnostics]}, ensure_ascii=False, indent=2))
        else:
            for item in diagnostics:
                print(f"{item.level} {item.code} {item.path or '-'} {item.message}")
        return 1 if any(item.level == "error" for item in diagnostics) else 0

    if args.command == "query":
        results = SearchIndex(repo_root()).query(args.text, args.stack, args.kind, args.limit)
        if args.format == "json":
            print(json.dumps({"results": [result.to_json() for result in results]}, ensure_ascii=False, indent=2))
        else:
            for result in results:
                print(
                    f"{result.chunk_id} "
                    f"{result.source_path}:{result.start_line}-{result.end_line} "
                    f"{result.summary}"
                )
        return 0

    if args.command == "read":
        reader = ChunkReader(repo_root())
        evidence = reader.read(args.chunk_id, args.with_neighbors)
        if args.format == "json":
            print(json.dumps(evidence, ensure_ascii=False, indent=2))
        else:
            print(reader.format(evidence, args.format))
        return 2 if evidence.get("stale") is True else 0

    if args.command == "preflight":
        result = PreflightRunner(repo_root()).run(args.task, args.stack, args.path)
        if args.format == "json":
            print(json.dumps(result, ensure_ascii=False, indent=2))
        else:
            print(f"status: {result['status']}")
            actions = result.get("actions", [])
            print(f"actions: {', '.join(actions) if actions else '-'}")
            for candidate in result.get("candidates", []):
                print(
                    f"{candidate['chunk_id']} "
                    f"{candidate['source_path']}:{candidate['start_line']}-{candidate['end_line']} "
                    f"{candidate['summary']}"
                )
            for diagnostic in result.get("diagnostics", []):
                print(
                    f"{diagnostic['level']} {diagnostic['code']} "
                    f"{diagnostic.get('path') or '-'} {diagnostic['message']}"
                )
        return 0 if result.get("status") == "ok" else 2

    if args.command == "postflight":
        changed_files = _split_changed_files(args.changed_files)
        result = PostflightRunner(repo_root()).run(changed_files)
        if args.format == "json":
            print(json.dumps(result, ensure_ascii=False, indent=2))
        else:
            actions = result.get("actions", [])
            if actions:
                for action in actions:
                    print(f"action: {action}")
            else:
                print("action: none")
            print(f"reason: {result['reason']}")
        return 0

    if args.command == "build-search":
        count = SearchIndex(repo_root()).build_fts()
        if args.format == "json":
            print(json.dumps({"backend": args.backend, "indexed_chunks": count}, ensure_ascii=False, indent=2))
        else:
            print(f"indexed chunks: {count}")
        return 0

    if args.command == "sync-vector":
        result = VectorSyncRunner(repo_root()).run(args.backend)
        if args.format == "json":
            print(json.dumps(result.to_json(), ensure_ascii=False, indent=2))
        else:
            print(f"{result.backend}: {result.status}")
        return result.exit_code

    parser.error(f"{args.command} is wired but not implemented in this task")
    return 2


def _split_changed_files(value: str) -> list[str]:
    return [part.strip() for group in value.split(";") for part in group.split(",") if part.strip()]
