---
name: tw-service-integration
description: Use when a requirement may involve service-to-service calls, public contracts, client proxies, auth boundaries, or remote service tooling in Tw.SmartPlatform.
---

# Service Integration

Use this before adding or changing cross-service calls.

## Checks

1. Query relevant caller and callee modules.
2. Query relevant contract nodes.
3. Query existing integration nodes.
4. Confirm whether `backend.capability.remote-service` is required.
5. Cite related standards, especially `rules.auth-oauth-oidc#rules`, `rules.resilience#rules`, and `rules.tracing#rules`.

## Output

Respond in Chinese with:

- caller
- callee
- contract
- protocol
- auth mode
- required tooling capability
- missing graph or contract declarations
