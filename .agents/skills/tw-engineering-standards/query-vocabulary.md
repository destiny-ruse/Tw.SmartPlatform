# Query Vocabulary

Map natural language and paths to routing metadata before reading shards.

| Signal | Route |
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

## Doc Type Hints

Use `rule` for implementation requirements, `reference` for catalogs and definitions, `process` for workflow questions, and `decision` for ADR-style context.

## Tag Hints

Use task nouns as tags when available, such as `api`, `caching`, `security`, `testing`, `observability`, `deployment`, `error-handling`, `contract`, and `governance`.
