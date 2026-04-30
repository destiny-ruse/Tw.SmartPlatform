$ErrorActionPreference = "Stop"

python tools\tw-memory\tw_memory.py check --format brief
git diff --check
