# Tw.MultiTenancy

`Tw.MultiTenancy` owns tenant identity contracts and tenant consistency resolution after authentication.

## Public Capabilities

- `CurrentTenant`
- `ICurrentTenant`
- `TenantResolver`
- `TenantMismatchException`

## Dependency Boundary

The package does not validate JWTs and does not access data stores.

## Usage

```csharp
var tenant = new TenantResolver().Resolve(tokenTenantId: "tenant-a", hintedTenantId: "tenant-a");
```
