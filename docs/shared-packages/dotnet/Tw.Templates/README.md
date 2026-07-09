# Tw.Templates

Official `dotnet new` templates for the Tw .NET framework.

## Install

```powershell
dotnet pack backend/dotnet/tools/Tw.Templates/Tw.Templates.csproj -o artifacts/templates
dotnet new install (Get-ChildItem artifacts/templates/Tw.Templates*.nupkg | Select-Object -First 1).FullName --force
```

## Templates

- `tw-service`: service solution with domain, application, HTTP API, and host projects.
- `tw-gateway`: gateway host skeleton.
- `tw-building-block`: shared package skeleton with package charter and tests.
- `tw-contract-package`: HTTP DTO, CAP event, proto, and error-code placeholders.

Templates must not emit retired framework package names.
