# Tw.Grpc

`Tw.Grpc` owns gRPC metadata propagation and deadline contracts.

## Public Capabilities

- `GrpcMetadataPropagationPolicy`
- `GrpcClientOptions`

## Dependency Boundary

The package documents contract-first `.proto` usage, stable field numbers, deadline propagation, trace/correlation/tenant/culture metadata, and error mapping. It does not host ASP.NET Core gRPC services.

## Usage

```csharp
var options = new GrpcClientOptions(TimeSpan.FromSeconds(3));
```
