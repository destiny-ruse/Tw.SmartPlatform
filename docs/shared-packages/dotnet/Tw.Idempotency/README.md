# Tw.Idempotency

`Tw.Idempotency` provides idempotency keys, reservation results, stable conflict errors, and host context factories.

## Usage

```csharp
var key = HttpIdempotencyContextFactory.Create("tenant-a", "Order", "Create", "request-1");
```
