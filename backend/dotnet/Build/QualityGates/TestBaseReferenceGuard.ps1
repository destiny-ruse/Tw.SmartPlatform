$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\BuildingBlocks\src")
$projects = Get-ChildItem -Path $root -Recurse -Filter *.csproj |
    Where-Object { $_.FullName -notmatch "\\src\\TestBase\\" }

$violations = foreach ($project in $projects) {
    $text = Get-Content -Raw -LiteralPath $project.FullName
    if ($text -match "Tw\..*TestBase" -or $text -match "\*TestBase") {
        $project.FullName
    }
}

if ($violations) {
    Write-Error ("Production projects must not reference TestBase packages: " + ($violations -join ", "))
}

Write-Host "TestBase reference guard passed."
