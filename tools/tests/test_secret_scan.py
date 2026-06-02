from __future__ import annotations

from tw_memory.secret_scan import scan_secrets


def test_scans_password_assignment_as_connection_string() -> None:
    assert scan_secrets("Password=hunter2;")[0].kind == "connection-string"


def test_detects_bearer_token() -> None:
    assert scan_secrets("Authorization: Bearer abcdef0123456789abcdef0123456789")[0].kind == "bearer-token"


def test_clean_text_has_no_hits() -> None:
    assert scan_secrets("Tw.Core public API card") == []


def test_detects_private_key_header() -> None:
    assert scan_secrets("-----BEGIN PRIVATE KEY-----")[0].kind == "private-key"


def test_secret_match_is_redacted() -> None:
    hit = scan_secrets("Password=hunter2;")[0]

    assert hit.match == "<redacted>"
