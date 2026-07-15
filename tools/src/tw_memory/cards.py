from __future__ import annotations

from tw_memory.charter import Charter
from tw_memory.implemented_api import PackageApi


def _bullets(items: list[str]) -> str:
    return "\n".join(f"- {item}" for item in items)


def _bullets_or_none(items: list[str]) -> str:
    return _bullets(items) if items else "- none"


def _inline_or_none(items: list[str]) -> str:
    normalized = [item.strip() for item in items if item.strip()]
    return ", ".join(normalized) if normalized else "none"


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
- forbid: {_inline_or_none(charter.dependency_rules.forbid)}
- allow: {_inline_or_none(charter.dependency_rules.allow)}

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
    implemented_api: PackageApi | None = None,
) -> str:
    """Render a generated public API card."""
    api = implemented_api or PackageApi([], [], [], [], [], [])
    return f"""# Public API: {package}

标识：{package} / {path}

公开能力边界：
{_bullets(charter.public_capabilities)}

实现公开命名空间：
{_bullets_or_none(api.public_namespaces)}

公开类型：
{_bullets_or_none([public_type.display for public_type in api.public_types])}

DI 注册入口：
{_bullets_or_none(api.di_registrations)}

包参考文档：
{_bullets_or_none(api.package_docs)}

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
{_bullets(source_refs)}
"""
