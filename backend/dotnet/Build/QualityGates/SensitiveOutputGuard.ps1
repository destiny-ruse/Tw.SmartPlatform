$ErrorActionPreference = "Stop"

$roots = @("artifacts", "backend/dotnet/TestResults") | Where-Object { Test-Path $_ }
$patterns = @("Password=", "Bearer\s+[A-Za-z0-9._-]+", "ConnectionStrings:")

foreach ($root in $roots) {
    $files = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue
    foreach ($pattern in $patterns) {
        $matches = $files | Select-String -Pattern $pattern -ErrorAction SilentlyContinue
        if ($matches) {
            Write-Error "Sensitive output pattern '$pattern' found under $root."
        }
    }
}

Write-Host "Sensitive output guard passed."
