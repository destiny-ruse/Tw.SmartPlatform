$ErrorActionPreference = "Stop"

$gatewayRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\BuildingBlocks\src\Gateway\Tw.Gateway.Yarp")
$forbiddenPatterns = @(
    "Tw\.Data",
    "Tw\.Uow",
    "Tw\.Application",
    "Tw\.EventBus",
    "Tw\.BackgroundJobs",
    "Tw\.MultiTenancy",
    "Tw\.Sharding"
)
$hits = @()

$files = Get-ChildItem -Path $gatewayRoot -Recurse -File -Include "*.cs" |
    Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" }

foreach ($file in $files) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $forbiddenPatterns) {
        if ($text -match $pattern) {
            $hits += "$($file.FullName): $pattern"
        }
    }
}

if ($hits.Count -gt 0) {
    $hits | ForEach-Object { Write-Error "Gateway package boundary violation: $_" }
    exit 1
}

Write-Host "Package boundary guard passed."
