# Tw.Analyzers

`Tw.Analyzers` provides compile-time governance diagnostics for the Tw .NET framework. The package has no runtime API and is packed under `analyzers/dotnet/cs`.

## Diagnostic index

| ID | Scope | Rule |
| --- | --- | --- |
| `TWGOV001` | C# declaration identifiers | Reports `Tw`, `Abp`, or `Furion` as a case-insensitive identifier segment. Segments are split at `_`, lower-to-upper transitions, and acronym boundaries. |

The sole `TWGOV001` exemption is the `Tw.Exceptions.TwException` type when it derives from `System.Exception` in the `Tw.Core` assembly.

## Governance boundaries

Retired-package checks and dependency-boundary checks belong to `Tw.Cli` and architecture tests until dedicated Roslyn diagnostics exist.
