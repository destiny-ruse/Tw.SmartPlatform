from __future__ import annotations

import re
from pathlib import Path

from tw_memory.generated_io import repo_relative

_FORMAL_REF = re.compile(r"`(docs/engineering-standards/[^`]+\.md)`")
_EXECUTION_REQUIREMENTS = re.compile(r"^##\s+Execution Requirements\s*$")
_HEADING = re.compile(r"^#{1,6}\s+")
_BULLET = re.compile(r"^\s*(?:[-*+]|\d+[.)])\s+(.*)$")
_EXACT_FOR_ALSO_LOAD_INSTRUCTIONS = (
    "For API changes, also load `tasks/api-design.md`.",
    "For authentication, authorization, sensitive data, or input boundary changes, also load `tasks/security.md`.",
    "For API, testing, security, dependency, CI/CD, runtime, or observability changes, also load the matching task index.",
    "For container runtime or Kubernetes deployment changes, also load `tasks/runtime-and-infrastructure.md`.",
    "For data, dependency, security, resilience, CI/CD, runtime, or observability changes, also load the matching task index.",
    "For deployment or rollback behavior, also load `tasks/ci-cd-and-release.md`.",
    "For message contracts or API idempotency, also load `tasks/api-design.md`.",
    "For release sequencing or rollback tasks, also load `tasks/ci-cd-and-release.md`.",
    "For runtime configuration or Kubernetes changes, also load `tasks/runtime-and-infrastructure.md`.",
    "For SLO or health check changes, also load `tasks/resilience-and-reliability.md`.",
)
_EXACT_READ_FORMAL_INSTRUCTIONS = (
    "Read the formal language standard before changing source in this technology.",
    "Read the formal coding standard before changing source.",
    "Read the formal API standard before changing API routes, request models, response models, error codes, status codes, message contracts, or generated clients.",
    "Read the formal CI/CD and release standard before changing pipeline or release behavior.",
    "Read the formal data and database standard before changing schema, migration files, data access models, data repair scripts, or data formats.",
    "Read the formal dependency and build standard before adding dependencies, changing lock files, changing build scripts, or changing artifact generation.",
    "Read the formal observability and operations standard before changing logs, metrics, tracing, alerts, or runbooks.",
    "Read the formal resilience and reliability standard before changing failure handling or external dependency behavior.",
    "Read the formal review and governance standard before reviewing or proposing high-risk changes.",
    "Read the formal runtime and infrastructure standard before changing environment, configuration, container, or Kubernetes resources.",
    "Read the formal security standard before changing security boundaries.",
    "Read the formal security standard before changing security boundaries or handling sensitive data.",
    "Read the formal testing standard before adding or changing tests.",
)
_EXACT_ALLOWED_BOUNDARY_INSTRUCTIONS = (
    "Read the referenced formal standard files before changing this task area.",
    "Load `00-always-load.md` together with this language index.",
    "If this task overlaps another task category, load the matching task index from `01-task-router.md`.",
    "Use `.tw-memory/routes/standards.generated.yaml` only to locate formal-standard sections.",
    "Use `.tw-memory/routes/standards.generated.yaml` only to locate formal-standard sections when the generated memory index is current.",
    "Do not infer rules from this index when a formal standard file is available.",
    "Treat `docs/engineering-standards` as the only source of engineering rules.",
    "Combine this baseline with one language index and any matching task indexes.",
    "If a task conflicts with a formal standard, follow the formal standard.",
    "If a task conflicts with a formal standard, follow the formal standard or record an exception according to the formal exception process.",
    "When `.tw-memory/routes/standards.generated.yaml` exists, use it only to locate formal-standard sections.",
    (
        "When `.tw-memory/routes/standards.generated.yaml` exists and matches `source-index.generated.json`, "
        "use it only to locate the relevant formal-standard sections; do not treat generated memory cards as engineering rules."
    ),
    "Do not load both a generated memory summary and the same formal standard text.",
    (
        "Do not load both a generated memory summary and the same formal standard text for the same rule. "
        "Prefer the formal standard text for decisions."
    ),
)


def _is_allowed_boundary_instruction(bullet_text: str) -> bool:
    return (
        bullet_text in _EXACT_ALLOWED_BOUNDARY_INSTRUCTIONS
        or bullet_text in _EXACT_READ_FORMAL_INSTRUCTIONS
        or bullet_text in _EXACT_FOR_ALSO_LOAD_INSTRUCTIONS
    )


def find_rule_files(root: Path) -> list[Path]:
    """Return all Markdown rule files under .rules."""
    rules_root = root / ".rules"
    if not rules_root.exists():
        return []
    return sorted(rules_root.rglob("*.md"))


def _rule_files(root: Path, rule_files: list[Path] | None) -> list[Path]:
    return rule_files if rule_files is not None else find_rule_files(root)


def find_formal_standard_refs(root: Path, rule_files: list[Path] | None = None) -> list[str]:
    """Extract formal engineering standard references from backticks."""
    refs: set[str] = set()
    for rule_file in _rule_files(root, rule_files):
        content = rule_file.read_text(encoding="utf-8")
        refs.update(_FORMAL_REF.findall(content))
    return sorted(refs)


def find_rules_boundary_violations(root: Path, rule_files: list[Path] | None = None) -> list[str]:
    """Find concrete engineering rule summaries embedded in .rules indexes."""
    violations: list[str] = []
    for rule_file in _rule_files(root, rule_files):
        in_execution_requirements = False
        relative_path = repo_relative(root, rule_file)

        for line_number, line in enumerate(rule_file.read_text(encoding="utf-8").splitlines(), start=1):
            if _EXECUTION_REQUIREMENTS.match(line):
                in_execution_requirements = True
                continue
            if in_execution_requirements and _HEADING.match(line):
                in_execution_requirements = False
            if not in_execution_requirements:
                continue

            bullet_match = _BULLET.match(line)
            if not bullet_match:
                if line.strip() and line[:1].isspace():
                    violations.append(f"{relative_path}:{line_number}: rule-summary")
                continue

            bullet_text = bullet_match.group(1).strip()
            if _is_allowed_boundary_instruction(bullet_text):
                continue

            violations.append(f"{relative_path}:{line_number}: rule-summary")

    return violations
