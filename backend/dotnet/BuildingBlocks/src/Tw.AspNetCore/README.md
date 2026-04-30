# Tw.AspNetCore

`Tw.AspNetCore` composes the BuildingBlocks ASP.NET Core infrastructure with
Autofac auto-registration, HTTP cancellation, and MVC/Web API Entry interception.

## WebApplication Setup

Call `AddTwAspNetCoreInfrastructure()` from `Program.cs` before building the app:

```csharp
using Tw.AspNetCore;
using Tw.DependencyInjection.Interception;

var builder = WebApplication.CreateBuilder(args);

builder.AddTwAspNetCoreInfrastructure(options => options
    .AddAssemblyOf<Program>()
    .EnableOptionsAutoRegistration()
    .AddGlobalInterceptor<RequestTracingInterceptor>(InterceptorScope.Entry)
    .AddGlobalInterceptor<ServiceTracingInterceptor>(InterceptorScope.Service));

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
await app.RunAsync();
```

The infrastructure entrypoint:

- sets Autofac as the service provider factory;
- calls `AddAutoRegistration()` and `UseAutoRegistration()`;
- registers `IHttpContextAccessor`;
- makes `HttpContext.RequestAborted` the effective request cancellation token;
- adds the MVC action filter that runs Entry interceptors.

## MVC Entry Interception

MVC Entry interception wraps controller action invocation. It does not wrap result
serialization after the action result leaves MVC action execution.

Entry chains are calculated from:

1. Global `InterceptorScope.Entry` registrations.
2. Matching `IInterceptorMatcher.MatchEntry(...)` registrations.

Inside an Entry interceptor, the invocation context exposes `IHttpRequestFeature`:

```csharp
public sealed class RequestTracingInterceptor : InterceptorBase
{
    public override async ValueTask InterceptAsync(IUnaryInvocationContext context)
    {
        var request = context.GetFeature<IHttpRequestFeature>()?.HttpContext.Request;
        await context.ProceedAsync();
    }
}
```

When an action has a `CancellationToken` argument, that token becomes the ambient
token for the Entry chain. Otherwise, `HttpContext.RequestAborted` is used.

## Service Registration And Options

The `configure` callback is the same `AutoRegistrationOptions` callback used by
`Tw.DependencyInjection`. Use it to add scan assemblies, enable options
registration, and register both Entry and Service interceptors.

Options auto-registration remains opt-in through `EnableOptionsAutoRegistration()`.

## Known Limits

- MVC Entry interception covers action invocation, not result serialization.
- gRPC is deferred to `P2-GRPC`.
- CAP and Worker adapters require separate specs before implementation.
- Method-level `[IgnoreInterceptors]` is out of scope.
- Open generic keyed services are rejected by `Tw.DependencyInjection`.

## Verification

Before releasing changes in this area, run:

```powershell
dotnet build backend/dotnet/Tw.SmartPlatform.slnx --no-restore
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj
dotnet test backend/dotnet/Tw.SmartPlatform.slnx
dotnet restore backend/dotnet/Tw.SmartPlatform.slnx --locked-mode
```
