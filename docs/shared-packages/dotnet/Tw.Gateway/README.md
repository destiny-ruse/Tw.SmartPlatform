# Tw.Gateway

`Tw.Gateway` owns route and header governance contracts. `Tw.Gateway.Yarp` must not depend on data, UoW, application, CAP, background jobs, tenancy runtime, or sharding runtime packages.

## Usage

```csharp
var sanitized = GatewayHeaderSanitizer.Sanitize(headers);
```
