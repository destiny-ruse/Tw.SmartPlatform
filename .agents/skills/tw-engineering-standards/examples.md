# Examples

## Cache Invalidation In A Backend Dotnet Path

Request:

```text
在 backend/dotnet/BuildingBlocks/src/Tw.Caching 里实现缓存失效功能
```

Inferred routes:

```text
roles: backend
stacks: dotnet
tags: caching, framework
doc_type: rule
```

Allowed reads:

```text
docs/standards/index.generated.json
docs/standards/_index/by-stack/dotnet.generated.json
docs/standards/_index/by-role/backend.generated.json
docs/standards/_index/by-tag/caching.generated.json
docs/standards/_index/by-tag/framework.generated.json
docs/standards/_index/sections/<candidate>.generated.json
docs/standards/<candidate>.md selected line ranges
```

Not allowed:

```text
docs/standards/**/*.md
docs/standards/_index/**/*.generated.json
```

Output must cite selected standards as:

```text
<standard-id>#<anchor>[:<region>] v<version> <path>
```
