from __future__ import annotations

from pathlib import Path

from tests.conftest import write_text
from tw_memory.rules_boundary import (
    find_formal_standard_refs,
    find_rules_boundary_violations,
)


def test_find_formal_standard_refs_extracts_backtick_refs(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/testing.md",
        "# Testing\n\n## Required Formal Standards\n\n"
        "- `docs/engineering-standards/04-quality/testing-standards.md`\n",
    )

    assert find_formal_standard_refs(repo, [rule_file]) == [
        "docs/engineering-standards/04-quality/testing-standards.md"
    ]


def test_find_formal_standard_refs_scans_rules_when_files_omitted(repo: Path) -> None:
    write_text(
        repo / ".rules/ai-coding-rules/tasks/testing.md",
        "# Testing\n\n## Required Formal Standards\n\n"
        "- `docs/engineering-standards/04-quality/testing-standards.md`\n",
    )

    assert find_formal_standard_refs(repo) == [
        "docs/engineering-standards/04-quality/testing-standards.md"
    ]


def test_find_rules_boundary_violations_flags_rule_summaries(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Never rely on front-end checks as the only authorization or validation control.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_non_keyword_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/testing.md",
        "# Testing\n\n## Execution Requirements\n"
        "- Core business logic must have automated tests.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/testing.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_numbered_rule_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "1. Never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_allows_loading_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changing security boundaries.\n"
        "- For API changes, also load `tasks/api-design.md`.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == []


def test_find_rules_boundary_violations_allows_read_flow_with_change_scope_list(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/api-design.md",
        "# API Design\n\n## Execution Requirements\n"
        "- Read the formal API standard before changing API routes, request models, response models, error codes, status codes, message contracts, or generated clients.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == []


def test_find_rules_boundary_violations_allows_task_index_loading_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the referenced formal standard files before changing this task area.\n"
        "- If this task overlaps another task category, load the matching task index from `01-task-router.md`.\n"
        "- Use `.tw-memory/routes/standards.generated.yaml` only to locate formal-standard sections.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == []


def test_find_rules_boundary_violations_allows_language_task_router_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/languages/typescript.md",
        "# TypeScript\n\n## Execution Requirements\n"
        "- Load `00-always-load.md` together with this language index.\n"
        "- Read the formal language standard before changing source in this technology.\n"
        "- For API, testing, security, dependency, CI/CD, runtime, or observability changes, also load the matching task index.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == []


def test_find_rules_boundary_violations_allows_memory_boundary_instructions(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/00-always-load.md",
        "# Always Load\n\n## Execution Requirements\n"
        "- Load `00-always-load.md` together with this language index.\n"
        "- Do not infer rules from this index when a formal standard file is available.\n"
        "- Treat `docs/engineering-standards` as the only source of engineering rules.\n"
        "- Combine this baseline with one language index and any matching task indexes.\n"
        "- If a task conflicts with a formal standard, follow the formal standard.\n"
        "- When `.tw-memory/routes/standards.generated.yaml` exists, use it only to locate formal-standard sections.\n"
        "- Do not load both a generated memory summary and the same formal standard text.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == []


def test_find_rules_boundary_violations_flags_load_rule_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Load passwords from protected runtime configuration.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_summary_appended_to_loading_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changes; never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_and_summary_after_read_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changes and never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_and_summary_after_changing_scope(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changing security boundaries and never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_comma_encrypt_summary_after_read_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changes, encrypt secrets.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_and_encrypt_summary_after_read_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changing security boundaries and encrypt secrets.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_comma_summary_after_read_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changes, never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_second_sentence_after_read_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changing security boundaries. Never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_indented_continuation_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- Read the formal security standard before changing security boundaries.\n"
        "  Never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:5: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_and_summary_after_for_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- For API changes, also load `tasks/api-design.md` and never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_summary_inside_for_scope(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- For API and never log tokens changes, also load the matching task index.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_and_summary_after_conflict_flow(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- If a task conflicts with a formal standard, follow the formal standard and never log tokens.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_when_use_rule_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- When handling tokens, use secure storage.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_when_rule_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- When handling tokens, never log secrets.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]


def test_find_rules_boundary_violations_flags_for_rule_summary(repo: Path) -> None:
    rule_file = write_text(
        repo / ".rules/ai-coding-rules/tasks/security.md",
        "# Security\n\n## Execution Requirements\n"
        "- For validation, reject unsafe input at the service boundary.\n",
    )

    assert find_rules_boundary_violations(repo, [rule_file]) == [
        ".rules/ai-coding-rules/tasks/security.md:4: rule-summary"
    ]
