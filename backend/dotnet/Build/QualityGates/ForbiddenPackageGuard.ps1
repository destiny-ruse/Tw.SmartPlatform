$ErrorActionPreference = "Stop"

$repository = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$scanRoots = @(
    (Join-Path $repository "backend\dotnet\BuildingBlocks\src"),
    (Join-Path $repository "backend\dotnet\tools\Tw.Templates\content")
)
$forbidden = @(
    "Tw.Infrastructure",
    "Tw.UnitOfWork",
    "Tw.Data.Abstractions",
    "MassTransit",
    "Tw.ObjectMapping",
    "Tw.ObjectMapping.AutoMapper"
)
$extensions = @(".cs", ".csproj", ".props", ".targets", ".json", ".proto")
$hits = @()

foreach ($root in $scanRoots) {
    if (-not (Test-Path -LiteralPath $root)) {
        continue
    }

    $files = Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $_.FullName -notmatch "\\(bin|obj)\\" -and
            $extensions -contains $_.Extension
        }

    foreach ($file in $files) {
        $text = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($name in $forbidden) {
            if ($text.Contains($name)) {
                $hits += "$($file.FullName): $name"
            }
        }
    }
}

if ($hits.Count -gt 0) {
    $hits | ForEach-Object { Write-Error "Forbidden package reference found: $_" }
    exit 1
}

Write-Host "Forbidden package guard passed."
