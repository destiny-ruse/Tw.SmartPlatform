# 查询词表

本文列出 v2 元数据允许的路由词表，以及常见自然语言到路由字段的映射。

## Roles

允许的 `roles`：

1. `architect`
2. `backend`
3. `frontend`
4. `qa`
5. `devops`
6. `ai`

## Stacks

允许的 `stacks`：

1. `dotnet`
2. `java`
3. `python`
4. `vue-ts`
5. `uniapp`

## Doc Types

允许的 `doc_type`：

1. `rule`
2. `reference`
3. `process`
4. `decision`

## Common Tags

M2-M5 内容批次会使用这些常见标签：

`api`, `rest`, `contract`, `response`, `error-handling`, `security`, `auth`, `oauth`, `oidc`, `governance`, `quality`, `naming`, `code-style`, `frontend`, `comments`, `repo-layout`, `editorconfig`, `grpc`, `asyncapi`, `messaging`, `testing`, `observability`, `operations`, `data`, `storage`, `backend`, `framework`, `cicd`, `deployment`, `standards`, `authoring`, `process`, `reference`, `decision`

## Natural Language Mapping

| 输入关键词 | 路由字段 |
| --- | --- |
| backend, api, service, controller, cache, database | roles backend |
| frontend, vue, component, page, route | roles frontend |
| test, qa, coverage, contract | roles qa |
| pipeline, docker, k8s, deploy | roles devops |
| architecture, RFC, ADR, governance | roles architect |
| dotnet, C#, .csproj | stacks dotnet |
| java, Maven, Gradle | stacks java |
| python, pyproject, pytest | stacks python |
| vue, TypeScript, Vite | stacks vue-ts |
| uni-app, mini program, mobile app | stacks uniapp |
