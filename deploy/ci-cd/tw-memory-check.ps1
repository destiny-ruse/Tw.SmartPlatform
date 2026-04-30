$ErrorActionPreference = "Stop"

python tools\tw-memory\tw_memory.py check --format brief

$diffBase = $env:TW_MEMORY_DIFF_BASE
if ([string]::IsNullOrWhiteSpace($diffBase) -and -not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF)) {
    $diffBase = "origin/$($env:GITHUB_BASE_REF)"
}

if ([string]::IsNullOrWhiteSpace($diffBase)) {
    git diff --check
}
else {
    git diff --check "$diffBase...HEAD"
    git diff --check
}
