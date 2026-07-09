# Tw.Http.Client

`Tw.Http.Client` defines outbound HTTP header propagation rules and HTTP resilience integration boundaries.

## Public Capabilities

- `HeaderPropagationPolicy`
- `HeaderTrustLevel`

## Dependency Boundary

`Authorization` is propagated only for user delegation or calls inside the same security boundary. `X-Tenant-Id` is propagated only after tenant verification.

## Usage

```csharp
var allowed = HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.Verified);
```
