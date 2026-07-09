$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\BuildingBlocks\src")
$projects = Get-ChildItem -Path $root -Recurse -Filter "*.csproj" -File |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }

$missing = @()
foreach ($project in $projects) {
    $charter = Join-Path $project.DirectoryName "package-charter.yaml"
    if (-not (Test-Path -LiteralPath $charter)) {
        $missing += $project.FullName
    }
}

if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Error "Missing package-charter.yaml for $_" }
    exit 1
}

Write-Host "Package charter guard passed."
