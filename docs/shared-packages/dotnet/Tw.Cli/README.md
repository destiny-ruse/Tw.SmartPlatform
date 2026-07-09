# Tw.Cli

`Tw.Cli` is the `tw` command line tool for project creation, dependency audit, contract validation, and repository diagnostics.

## Install

```powershell
dotnet pack backend/dotnet/tools/Tw.Cli/Tw.Cli.csproj -o artifacts/tools
dotnet tool install --tool-path artifacts/tool-home Tw.Cli --add-source artifacts/tools
```

## Commands

- `tw diagnose --repository <path>` reports package topology, central package drift, and lock file status.
- `tw audit dependencies --repository <path>` runs dependency governance checks and returns a non-zero exit code on violations.
- `tw validate contracts --repository <path>` runs contract validation entry points.

## Error Codes

- `TWGOV000`: invalid input or invalid project file.
- `TWGOV002`: forbidden package reference.
- `TWGOV003`: production project references a test-only package.
