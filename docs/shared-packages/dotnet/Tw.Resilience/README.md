# Tw.Resilience

`Tw.Resilience` defines resilience descriptors and retry safety rules.

## Usage

```csharp
var policy = ResiliencePolicyBuilder.Build(
    ResiliencePolicyDescriptor.ForHttp("CreateOrder", OperationKind.NonIdempotentWrite, TimeSpan.FromSeconds(3)));
```
