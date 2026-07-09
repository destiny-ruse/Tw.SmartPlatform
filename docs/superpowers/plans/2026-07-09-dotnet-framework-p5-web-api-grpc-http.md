# Dotnet Framework P5 Web API GRPC HTTP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement ASP.NET Core, MVC, Newtonsoft JSON, Swashbuckle, gRPC, HTTP client, and localization host integration packages.

**Architecture:** Protocol adapters stay outside the application layer. HTTP and gRPC map protocol errors to stable error responses, enforce long ID string contracts at the protocol boundary, propagate only trusted headers, and reuse the shared application pipeline when entering use cases.

**Tech Stack:** ASP.NET Core 10, Microsoft.AspNetCore.Mvc.NewtonsoftJson, Swashbuckle, Asp.Versioning.Mvc, gRPC, NSwag.MSBuild, Microsoft.Extensions.Http.Resilience, Newtonsoft.Json

---

## File Structure

- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc.NewtonsoftJson`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Swashbuckle`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Grpc`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization`
- Create: `backend/dotnet/BuildingBlocks/src/Grpc/Tw.Grpc`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Abstractions`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Client`
- Create matching tests under `backend/dotnet/BuildingBlocks/tests`

### Task 1: Create ASP.NET Core Protocol Abstractions

**Files:**
- Create: `Tw.AspNetCore.Abstractions/ProtocolError.cs`
- Create: `Tw.AspNetCore.Abstractions/RequestCorrelation.cs`
- Create: `Tw.AspNetCore.Abstractions/AuthenticationSchemeNames.cs`
- Create: `Tw.AspNetCore.Abstractions.Tests/ProtocolErrorTests.cs`

- [ ] **Step 1: Write protocol error test**

```csharp
using AwesomeAssertions;
using Tw.AspNetCore.Abstractions;

namespace Tw.AspNetCore.Abstractions.Tests;

public sealed class ProtocolErrorTests
{
    [Fact]
    public void Conflict_UsesHttp409()
    {
        var error = ProtocolError.Conflict("DATA:CONFLICT", "数据已被其他请求修改");

        error.StatusCode.Should().Be(409);
        error.Code.Should().Be("DATA:CONFLICT");
    }
}
```

- [ ] **Step 2: Implement protocol error**

```csharp
namespace Tw.AspNetCore.Abstractions;

public sealed record ProtocolError(int StatusCode, string Code, string Message, string? TraceId)
{
    public static ProtocolError Conflict(string code, string message, string? traceId = null)
    {
        return new ProtocolError(409, code, message, traceId);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Abstractions.Tests/Tw.AspNetCore.Abstractions.Tests.csproj`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Abstractions.Tests
git commit -m "feat: add aspnetcore protocol abstractions"
```

### Task 2: Implement Long ID HTTP Binding And Newtonsoft JSON

**Files:**
- Create: `Tw.AspNetCore.Mvc.NewtonsoftJson/LongIdJsonConverter.cs`
- Create: `Tw.AspNetCore.Mvc/ModelBinding/LongIdModelBinder.cs`
- Create: `Tw.AspNetCore.Mvc.Tests/ModelBinding/LongIdModelBinderTests.cs`
- Create: `Tw.AspNetCore.Mvc.NewtonsoftJson.Tests/LongIdJsonConverterTests.cs`

- [ ] **Step 1: Write JSON converter test**

```csharp
using AwesomeAssertions;
using Newtonsoft.Json;
using Tw.AspNetCore.Mvc.NewtonsoftJson;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson.Tests;

public sealed class LongIdJsonConverterTests
{
    private sealed record Sample(long Id);

    [Fact]
    public void Serialize_WritesLongAsString()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new LongIdJsonConverter());

        var json = JsonConvert.SerializeObject(new Sample(9007199254740993L), settings);

        json.Should().Contain("\"Id\":\"9007199254740993\"");
    }
}
```

- [ ] **Step 2: Write model binder test**

```csharp
using AwesomeAssertions;
using Tw.AspNetCore.Mvc.ModelBinding;

namespace Tw.AspNetCore.Mvc.Tests.ModelBinding;

public sealed class LongIdModelBinderTests
{
    [Fact]
    public void TryParse_ReturnsFalse_WhenValueExceedsLong()
    {
        LongIdModelBinder.TryParse("999999999999999999999999", out _)
            .Should()
            .BeFalse();
    }
}
```

- [ ] **Step 3: Implement model binder parsing method**

```csharp
namespace Tw.AspNetCore.Mvc.ModelBinding;

public sealed class LongIdModelBinder
{
    public static bool TryParse(string? value, out long id)
    {
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/Tw.SmartPlatform.slnx --filter "FullyQualifiedName~LongId"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc.NewtonsoftJson backend/dotnet/BuildingBlocks/tests
git commit -m "feat: add http long id contract handling"
```

### Task 3: Implement Unified Response And CSRF Rules

**Files:**
- Create: `Tw.AspNetCore.Mvc/Responses/ApiResponse.cs`
- Create: `Tw.AspNetCore.Mvc/Responses/ApiErrorResponseFactory.cs`
- Create: `Tw.AspNetCore.Mvc/Security/AntiforgeryPolicy.cs`
- Create: `Tw.AspNetCore.Mvc.Tests/Security/AntiforgeryPolicyTests.cs`

- [ ] **Step 1: Write antiforgery policy test**

```csharp
using AwesomeAssertions;
using Tw.AspNetCore.Mvc.Security;

namespace Tw.AspNetCore.Mvc.Tests.Security;

public sealed class AntiforgeryPolicyTests
{
    [Fact]
    public void RequiresValidation_ReturnsFalse_ForBearerGetRequest()
    {
        AntiforgeryPolicy.RequiresValidation("GET", "Bearer")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void RequiresValidation_ReturnsTrue_ForCookiePostRequest()
    {
        AntiforgeryPolicy.RequiresValidation("POST", "Cookies")
            .Should()
            .BeTrue();
    }
}
```

- [ ] **Step 2: Implement policy**

```csharp
namespace Tw.AspNetCore.Mvc.Security;

public static class AntiforgeryPolicy
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "HEAD",
        "OPTIONS",
        "TRACE"
    };

    public static bool RequiresValidation(string method, string authenticationScheme)
    {
        return !SafeMethods.Contains(method) && string.Equals(authenticationScheme, "Cookies", StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 3: Implement response model**

```csharp
namespace Tw.AspNetCore.Mvc.Responses;

public sealed record ApiResponse<T>(bool Success, string Code, string Message, T? Data, string? TraceId, string? CorrelationId, DateTimeOffset Timestamp);
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --filter "AntiforgeryPolicyTests|ApiResponse"`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests
git commit -m "feat: add mvc response and antiforgery contracts"
```

### Task 4: Implement Swashbuckle Long ID Schema Mapping

**Files:**
- Create: `Tw.AspNetCore.Swashbuckle/LongIdSchemaFilter.cs`
- Create: `Tw.AspNetCore.Swashbuckle/OpenApiServiceCollectionExtensions.cs`
- Create: `Tw.AspNetCore.Swashbuckle/OpenApiRegistrationOptions.cs`
- Create: `Tw.AspNetCore.Swashbuckle/JwtSecurityDefinitionOperationFilter.cs`
- Create: `Tw.AspNetCore.Swashbuckle/ApiResponseOperationFilter.cs`
- Create: `Tw.AspNetCore.Swashbuckle.Tests/LongIdSchemaFilterTests.cs`
- Create: `Tw.AspNetCore.Swashbuckle.Tests/OpenApiServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write schema filter test**

```csharp
using AwesomeAssertions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Tw.AspNetCore.Swashbuckle;

namespace Tw.AspNetCore.Swashbuckle.Tests;

public sealed class LongIdSchemaFilterTests
{
    [Fact]
    public void Apply_MapsLongToStringInt64()
    {
        var schema = new OpenApiSchema { Type = "integer", Format = "int64" };
        var filter = new LongIdSchemaFilter();

        filter.Apply(schema, new SchemaFilterContext(typeof(long), null, null, null));

        schema.Type.Should().Be("string");
        schema.Format.Should().Be("int64");
        schema.Extensions.Should().ContainKey("x-tw-id");
    }
}
```

- [ ] **Step 2: Implement schema filter**

```csharp
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Tw.AspNetCore.Swashbuckle;

public sealed class LongIdSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(long) || context.Type == typeof(long?))
        {
            schema.Type = "string";
            schema.Format = "int64";
            schema.Extensions["x-tw-id"] = new OpenApiBoolean(true);
        }
    }
}
```

- [ ] **Step 3: Implement OpenAPI registration**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Tw.AspNetCore.Swashbuckle;

public sealed record OpenApiRegistrationOptions(
    string DocumentName,
    string Title,
    string Version,
    IReadOnlyList<string> XmlCommentFiles);

public static class OpenApiServiceCollectionExtensions
{
    public static IServiceCollection AddOpenApiIntegration(
        this IServiceCollection services,
        OpenApiRegistrationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSwaggerGen(setup =>
        {
            setup.SwaggerDoc(options.DocumentName, new OpenApiInfo { Title = options.Title, Version = options.Version });
            setup.SchemaFilter<LongIdSchemaFilter>();
            setup.OperationFilter<JwtSecurityDefinitionOperationFilter>();
            setup.OperationFilter<ApiResponseOperationFilter>();

            foreach (var xmlCommentFile in options.XmlCommentFiles)
            {
                setup.IncludeXmlComments(xmlCommentFile);
            }
        });

        services.AddSwaggerGenNewtonsoftSupport();
        return services;
    }
}
```

`JwtSecurityDefinitionOperationFilter` must add JWT Bearer security metadata. `ApiResponseOperationFilter` must document stable error responses, unified response wrapping, enum descriptions, error code response metadata, and versioned document grouping.

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Swashbuckle.Tests/Tw.AspNetCore.Swashbuckle.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Swashbuckle backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Swashbuckle.Tests
git commit -m "feat: add swashbuckle long id schema mapping"
```

### Task 5: Implement HTTP Client Header Propagation Rules

**Files:**
- Create: `Tw.Http.Abstractions/HeaderPropagationOptions.cs`
- Create: `Tw.Http.Client/HeaderPropagation/HeaderPropagationPolicy.cs`
- Create: `Tw.Http.Client.Tests/HeaderPropagationPolicyTests.cs`

- [ ] **Step 1: Write header propagation test**

```csharp
using AwesomeAssertions;
using Tw.Http.Client.HeaderPropagation;

namespace Tw.Http.Client.Tests;

public sealed class HeaderPropagationPolicyTests
{
    [Fact]
    public void ShouldPropagate_DoesNotPropagateClientTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.ClientSupplied)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void ShouldPropagate_PropagatesVerifiedTenantHeader()
    {
        HeaderPropagationPolicy.ShouldPropagate("X-Tenant-Id", HeaderTrustLevel.Verified)
            .Should()
            .BeTrue();
    }
}
```

- [ ] **Step 2: Implement policy**

```csharp
namespace Tw.Http.Client.HeaderPropagation;

public enum HeaderTrustLevel
{
    ClientSupplied,
    Verified
}

public static class HeaderPropagationPolicy
{
    private static readonly HashSet<string> AllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "traceparent",
        "tracestate",
        "X-Correlation-Id",
        "X-Tenant-Id",
        "X-Culture",
        "Idempotency-Key"
    };

    public static bool ShouldPropagate(string headerName, HeaderTrustLevel trustLevel)
    {
        return AllowList.Contains(headerName)
            && (!string.Equals(headerName, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase) || trustLevel == HeaderTrustLevel.Verified);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Http.Client.Tests/Tw.Http.Client.Tests.csproj --filter HeaderPropagationPolicyTests`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Http backend/dotnet/BuildingBlocks/tests/Tw.Http.Client.Tests
git commit -m "feat: add trusted header propagation policy"
```

### Task 6: Implement gRPC Metadata And Deadline Contracts

**Files:**
- Create: `Tw.Grpc/GrpcClientOptions.cs`
- Create: `Tw.Grpc/GrpcMetadataPropagationPolicy.cs`
- Create: `Tw.Grpc.Tests/GrpcMetadataPropagationPolicyTests.cs`

- [ ] **Step 1: Write metadata propagation test**

```csharp
using AwesomeAssertions;
using Tw.Grpc;

namespace Tw.Grpc.Tests;

public sealed class GrpcMetadataPropagationPolicyTests
{
    [Fact]
    public void AllowedMetadata_IncludesTraceTenantCultureAndAuthorization()
    {
        GrpcMetadataPropagationPolicy.AllowedMetadata
            .Should()
            .BeEquivalentTo("traceparent", "tracestate", "correlation-id", "tenant-id", "culture", "authorization");
    }
}
```

- [ ] **Step 2: Implement policy**

```csharp
namespace Tw.Grpc;

public static class GrpcMetadataPropagationPolicy
{
    public static IReadOnlySet<string> AllowedMetadata { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "traceparent",
        "tracestate",
        "correlation-id",
        "tenant-id",
        "culture",
        "authorization"
    };
}

public sealed record GrpcClientOptions(TimeSpan Deadline);
```

- [ ] **Step 3: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Grpc.Tests/Tw.Grpc.Tests.csproj`

Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Grpc/Tw.Grpc backend/dotnet/BuildingBlocks/tests/Tw.Grpc.Tests
git commit -m "feat: add grpc metadata and deadline contracts"
```

### Task 7: Rename Localization ASP.NET Core Namespace

**Files:**
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/**/*.cs`
- Modify: `backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests`
- Rename test project to `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Localization.Tests`

- [ ] **Step 1: Replace namespace**

Replace:

```csharp
namespace Tw.Localization.AspNetCore;
```

with:

```csharp
namespace Tw.AspNetCore.Localization;
```

- [ ] **Step 2: Rename test project**

Run:

```powershell
Move-Item -LiteralPath backend/dotnet/BuildingBlocks/tests/Tw.Localization.AspNetCore.Tests -Destination backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Localization.Tests
Rename-Item -LiteralPath backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Localization.Tests/Tw.Localization.AspNetCore.Tests.csproj -NewName Tw.AspNetCore.Localization.Tests.csproj
```

- [ ] **Step 3: Update project references**

The test project references:

```xml
<ProjectReference Include="..\..\src\Web\Tw.AspNetCore.Localization\Tw.AspNetCore.Localization.csproj" />
```

- [ ] **Step 4: Run tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Localization.Tests/Tw.AspNetCore.Localization.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Localization.Tests backend/dotnet/Tw.SmartPlatform.slnx
git commit -m "refactor: rename aspnetcore localization package"
```

### Task 8: Implement ASP.NET Core Host Governance And API Versioning

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Security/AuthenticationBoundaryOptions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/RateLimiting/RateLimitServiceCollectionExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/Health/HealthEndpointRouteBuilderExtensions.cs`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/ApiVersioning/ApiVersioningServiceCollectionExtensions.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Middleware/ExceptionHandlingMiddlewareTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Security/AuthenticationBoundaryOptionsTests.cs`
- Test: `backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/ApiVersioning/ApiVersioningServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write API Versioning test**

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tw.AspNetCore.Mvc.ApiVersioning;

namespace Tw.AspNetCore.Mvc.Tests.ApiVersioning;

public sealed class ApiVersioningServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApiVersioningIntegration_RegistersUrlSegmentVersioning()
    {
        var services = new ServiceCollection();

        services.AddApiVersioningIntegration();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType.FullName!.Contains("IApiVersionReader", StringComparison.Ordinal));
    }
}
```

- [ ] **Step 2: Implement MVC API Versioning integration**

```csharp
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Tw.AspNetCore.Mvc.ApiVersioning;

public static class ApiVersioningServiceCollectionExtensions
{
    public static IServiceCollection AddApiVersioningIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
```

- [ ] **Step 3: Write authentication boundary test**

```csharp
using AwesomeAssertions;
using Tw.AspNetCore.Security;

namespace Tw.AspNetCore.Tests.Security;

public sealed class AuthenticationBoundaryOptionsTests
{
    [Fact]
    public void Validate_RejectsMissingIssuer()
    {
        var options = new AuthenticationBoundaryOptions(
            ValidIssuer: "",
            ValidAudience: "billing-api",
            RequiredScopes: ["billing.read"]);

        var act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT issuer 必须配置");
    }
}
```

- [ ] **Step 4: Implement host governance contracts**

```csharp
namespace Tw.AspNetCore.Security;

public sealed record AuthenticationBoundaryOptions(
    string ValidIssuer,
    string ValidAudience,
    IReadOnlyList<string> RequiredScopes)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ValidIssuer))
        {
            throw new InvalidOperationException("JWT issuer 必须配置");
        }

        if (string.IsNullOrWhiteSpace(ValidAudience))
        {
            throw new InvalidOperationException("JWT audience 必须配置");
        }
    }
}
```

`ExceptionHandlingMiddleware` maps framework exceptions to unified error responses with real HTTP status codes. `RateLimitServiceCollectionExtensions` registers application-level rate limiting policies only. `HealthEndpointRouteBuilderExtensions` maps health endpoints that are excluded from unified response wrapping.

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests/Tw.AspNetCore.Tests.csproj --filter "ExceptionHandling|AuthenticationBoundary|RateLimit|Health" --nologo
dotnet test backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests/Tw.AspNetCore.Mvc.Tests.csproj --filter ApiVersioning --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Tests backend/dotnet/BuildingBlocks/tests/Tw.AspNetCore.Mvc.Tests
git commit -m "feat: add aspnetcore host governance and api versioning"
```

### Task 9: Add Web, HTTP, And gRPC Charters

**Files:**
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Abstractions/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Mvc.NewtonsoftJson/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Swashbuckle/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Grpc/package-charter.yaml`
- Modify: `backend/dotnet/BuildingBlocks/src/Web/Tw.AspNetCore.Localization/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Grpc/Tw.Grpc/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Abstractions/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http/package-charter.yaml`
- Create: `backend/dotnet/BuildingBlocks/src/Http/Tw.Http.Client/package-charter.yaml`
- Create: `docs/shared-packages/dotnet/Tw.AspNetCore.Swashbuckle/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Http.Client/README.md`
- Create: `docs/shared-packages/dotnet/Tw.Grpc/README.md`
- Modify: `docs/shared-packages/dotnet/README.md`

- [ ] **Step 1: Add ASP.NET Core charter**

`Tw.AspNetCore/package-charter.yaml` must include:

```yaml
schema_version: "1.0.0"
package: Tw.AspNetCore
owner: platform-team
stability: experimental
compatibility: "experimental 阶段不承诺兼容"
responsibility: >
  HTTP 宿主集成、中间件、异常处理、认证、限流和健康端点。
in_scope:
  - HTTP 宿主集成
  - 异常处理中间件
  - 认证边界配置
  - 应用级限流
  - 健康端点
out_of_scope:
  - MVC Filter
  - Newtonsoft.Json MVC 配置
  - Swashbuckle OpenAPI
  - gRPC 服务端注册
public_capabilities:
  - Tw.AspNetCore
dependency_rules:
  allow:
    - "Tw.AspNetCore.Abstractions"
    - "Tw.ExceptionHandling"
    - "Tw.Observability"
  forbid:
    - "SqlSugar*"
    - "DotNetCore.CAP*"
    - "Quartz"
```

- [ ] **Step 2: Add Swashbuckle and MVC charters**

`Tw.AspNetCore.Swashbuckle/package-charter.yaml` must list Swashbuckle registration, Newtonsoft support, JWT security definitions, XML comments, enum/error/unified response descriptions, grouped version documents, operation filters, schema filters, and ID string mapping. `Tw.AspNetCore.Mvc/package-charter.yaml` must list MVC Filter, Endpoint Filter, unified response, model binding errors, API Versioning URL Segment registration, CSRF/XSRF, and antiforgery validation.

- [ ] **Step 3: Add HTTP and gRPC charters**

`Tw.Http.Client/package-charter.yaml` must state that `Authorization` is propagated only for user delegation or same security boundary calls, and that `X-Tenant-Id` is propagated only after tenant verification. `Tw.Grpc/package-charter.yaml` must state contract-first `.proto`, stable field numbers, deadline propagation, trace/correlation/tenant/culture metadata, and error mapping.

- [ ] **Step 4: Add shared-package docs and index links**

Docs must include registration examples for `AddApiVersioningIntegration()`, `AddOpenApiIntegration(...)`, HTTP header propagation, and gRPC metadata propagation.

- [ ] **Step 5: Run architecture tests**

Run: `dotnet test backend/dotnet/BuildingBlocks/tests/Tw.Architecture.Tests/Tw.Architecture.Tests.csproj --filter PackageCharterTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/dotnet/BuildingBlocks/src/Web backend/dotnet/BuildingBlocks/src/Grpc backend/dotnet/BuildingBlocks/src/Http docs/shared-packages/dotnet
git commit -m "docs: add web http and grpc package charters"
```

## Plan Self-Review

- Spec coverage: ASP.NET Core abstractions, host exception/authentication/rate-limit/health boundaries, MVC response, API Versioning URL Segment, CSRF/XSRF, Newtonsoft long ID, Swashbuckle JWT/XML/version/error/response and long ID schema mapping, HTTP header propagation, gRPC metadata, localization rename, package charters, and shared-package docs are covered.
- Placeholder scan: no placeholder tokens are present.
- Type consistency: package names use final Web/Http/Grpc names.
