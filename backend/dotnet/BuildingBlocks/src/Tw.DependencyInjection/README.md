# Tw.DependencyInjection

`Tw.DependencyInjection` provides automatic service registration, options registration,
ambient cancellation, invocation context, and Service-scope AOP for BuildingBlocks.
`Tw.Core` remains free of Autofac, Castle, ASP.NET Core, and gRPC dependencies.

## Host Setup

Use the package from a host that chooses Autofac as its service provider:

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Tw.DependencyInjection.Interception;
using Tw.DependencyInjection.Registration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAutoRegistration(
    builder.Configuration,
    options => options
        .AddAssemblyOf<Program>()
        .EnableOptionsAutoRegistration()
        .AddGlobalInterceptor<TracingInterceptor>(InterceptorScope.Service));

builder.ConfigureContainer(
    new AutofacServiceProviderFactory(),
    containerBuilder => containerBuilder.UseAutoRegistration(builder.Services));

using var host = builder.Build();
await host.RunAsync();
```

`AddAutoRegistration()` must run before `UseAutoRegistration()`. The service
collection stores the scan options and the Autofac container consumes them later.

## Service Registration

Types are candidates when they implement exactly one lifecycle marker:

- `ITransientDependency`
- `IScopedDependency`
- `ISingletonDependency`

Default exposure registers all non-`System.*` and non-`Microsoft.*` interfaces,
excluding lifecycle markers. If no business interface remains, the implementation
type is registered as itself.

Use registration attributes for explicit cases:

- `[ExposeServices(typeof(IMyService), IncludeSelf = true)]` controls service exposure.
- `[DisableAutoRegistration]` removes a type from scanning.
- `[ReplaceService(Order = 10)]` participates in deterministic replacement.
- `[CollectionService(Order = 10)]` appends to `IEnumerable<T>` registrations.
- `[KeyedService("name")]` registers an Autofac keyed service.

Open generic keyed services are rejected at startup. This is intentional for the
first delivery batch because the key semantics need a separate design.

## Options Registration

Options registration is opt-in:

```csharp
services.AddAutoRegistration(
    configuration,
    options => options
        .AddAssemblyOf<Program>()
        .EnableOptionsAutoRegistration());
```

Option classes can use `ConfigurationSectionAttribute`:

```csharp
[ConfigurationSection("Payments", DirectInject = true)]
public sealed class PaymentOptions : IConfigurableOptions
{
    public required string MerchantId { get; init; }
}
```

Each declaration registers Microsoft Options, binds the named section, applies
data-annotation validation, and validates on host startup by default. Set
`ValidateOnStart = false` only for configuration that cannot be resolved at
startup. Direct injection of `TOptions` is only registered for the default
options instance; named options remain available through Microsoft Options APIs.

## Service AOP

Service interception uses Castle interface proxies. The chain order is:

1. Global `InterceptorScope.Service` registrations.
2. Matching `IInterceptorMatcher.MatchService(...)` registrations.
3. Explicit `[Intercept]` registrations.

Each layer is sorted by `Order`, then interceptor type full name. `[IgnoreInterceptors]`
suppresses global and matcher interceptors, but explicit `[Intercept]` remains
authoritative.

Implement interceptors by deriving from `InterceptorBase`:

```csharp
public sealed class TracingInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        await context.ProceedAsync();
    }
}
```

Only interface services are proxied by default. Concrete-only services with a
Service chain are registered without a Castle proxy and produce a non-fatal
warning.

## Diagnostics

Development diagnostics are enabled automatically when configuration contains
`DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT`, or `environment` with the value
`Development`. Production stays silent unless diagnostics are explicitly enabled.

```csharp
services.AddAutoRegistration(
    configuration,
    options => options
        .AddAssemblyOf<Program>()
        .EnableDiagnostics()
        .WriteDiagnosticsTo(message => logger.LogInformation("{Message}", message)));
```

Diagnostics include scan, sort, AOP, register, and total elapsed timings, service
counts, intercepted counts, and warnings. Use `EnableDiagnostics(false)` to
suppress output even in development.

## Known Limits

- MVC/Web API Entry interception lives in `Tw.AspNetCore`.
- gRPC is deferred to `P2-GRPC`.
- CAP and Worker adapters require separate specs before implementation.
- Method-level `[IgnoreInterceptors]` is out of scope.
- Open generic keyed services are rejected at startup.

## Verification

Before releasing changes in this area, run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Core.Tests/Tw.Core.Tests.csproj
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.DependencyInjection.Tests/Tw.DependencyInjection.Tests.csproj
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
```
