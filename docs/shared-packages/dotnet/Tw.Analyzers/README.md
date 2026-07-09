# Tw.Analyzers

`Tw.Analyzers` provides compile-time governance diagnostics for the Tw .NET framework. The package has no runtime API and is packed under `analyzers/dotnet/cs`.

## Diagnostics

- `TWGOV001`: self-owned identifiers must not use framework-owned prefixes.
- `TWGOV002`: forbidden package names and retired package names are errors.
- `TWGOV003`: production projects must not reference `*TestBase` packages.
- `TWGOV004`: business projects must not directly reference implementation packages outside allowed layers.
- `TWGOV005`: User Secrets are limited to local and development entry points.
- `TWGOV006`: external HTTP JSON, OpenAPI, and generated client contracts expose long IDs as decimal strings.

`TwException` is the allowed exception for `TWGOV001`.
