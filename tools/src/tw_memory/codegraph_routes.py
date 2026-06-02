from __future__ import annotations


def default_codegraph_queries() -> dict[str, object]:
    """Return non-blocking CodeGraph query intents."""
    return {
        "queries": {
            "find_symbol": {"requires": ["symbol"], "verify_with_source": True},
            "callers": {"requires": ["symbol"], "verify_with_source": True},
            "callees": {"requires": ["symbol"], "verify_with_source": True},
            "impact": {"requires": ["path"], "verify_with_source": True},
            "route_handlers": {"requires": ["api"], "verify_with_source": True},
        }
    }
