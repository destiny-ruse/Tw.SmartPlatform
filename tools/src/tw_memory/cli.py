from __future__ import annotations

import argparse


def main(argv: list[str] | None = None) -> int:
    """Run the tw-memory command line interface.

    Args:
        argv: Command arguments without the executable name. When omitted,
            argparse reads from the current process arguments.

    Returns:
        Process exit code for the selected command.
    """
    parser = argparse.ArgumentParser(prog="tw-memory")
    sub = parser.add_subparsers(dest="command", required=True)

    gen = sub.add_parser("generate", help="Generate .tw-memory from source facts.")
    gen.add_argument("--root", default=None, help="Repository root. Defaults to auto-detect.")

    chk = sub.add_parser("check", help="Validate generated memory and source facts.")
    chk.add_argument("--root", default=None, help="Repository root. Defaults to auto-detect.")
    chk.add_argument("--staged", action="store_true", help="Check git-staged paths for commit-boundary gates.")

    args = parser.parse_args(argv)

    if args.command == "generate":
        from tw_memory.generate import run_generate

        return run_generate(args.root)
    if args.command == "check":
        from tw_memory.check import run_check

        return run_check(args.root, staged=args.staged)
    parser.error(f"unknown command {args.command!r}")
    return 2
