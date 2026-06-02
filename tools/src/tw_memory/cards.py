from __future__ import annotations

from tw_memory.charter import Charter


def _bullets(items: list[str]) -> str:
    return "\n".join(f"- {item}" for item in items)


def render_package_card(
    package: str,
    path: str,
    charter: Charter,
    source_refs: list[str],
) -> str:
    """Render a generated package card."""
    compatibility = charter.compatibility or ""
    migration = charter.migration_ref or ""
    return f"""# Package: {package}

标识：{package} / {path} / {charter.owner}
职责：{charter.responsibility}

适用范围：
{_bullets(charter.in_scope)}

不适用范围：
{_bullets(charter.out_of_scope)}

依赖边界：
- forbid: {", ".join(charter.dependency_rules.forbid)}
- allow: {", ".join(charter.dependency_rules.allow)}

稳定性：{charter.stability}
兼容性：{compatibility}
迁移指针：{migration}

source_refs:
{_bullets(source_refs)}
"""


def render_public_api_card(
    package: str,
    path: str,
    charter: Charter,
    source_refs: list[str],
) -> str:
    """Render a generated public API card."""
    return f"""# Public API: {package}

标识：{package} / {path}

公开能力：
{_bullets(charter.public_capabilities)}

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml

source_refs:
{_bullets(source_refs)}
"""
