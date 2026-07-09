# Tw.AspNetCore.Swashbuckle

`Tw.AspNetCore.Swashbuckle` registers OpenAPI documents, stable error response metadata, JWT operation metadata, Newtonsoft support, XML comments, and long ID string schema mapping.

## Public Capabilities

- `AddOpenApiIntegration(...)`
- `LongIdSchemaFilter`
- `JwtSecurityDefinitionOperationFilter`
- `ApiResponseOperationFilter`

## Dependency Boundary

The package owns documentation-time OpenAPI behavior only. Runtime authentication, MVC model binding, and generated clients stay outside this package.

## Usage

```csharp
services.AddOpenApiIntegration(new OpenApiRegistrationOptions("v1", "Billing API", "v1", []));
```
