# Tw.AspNetCore.Abstractions

`Tw.AspNetCore.Abstractions` contains protocol boundary contracts shared by ASP.NET Core adapters.

## Public Capabilities

- `ProtocolError`
- `RequestCorrelation`
- `AuthenticationSchemeNames`

## Dependency Boundary

The package does not reference MVC, Swashbuckle, SqlSugar, or CAP.

## Usage

```csharp
var error = ProtocolError.Conflict("DATA:CONFLICT", "Data changed.");
```
