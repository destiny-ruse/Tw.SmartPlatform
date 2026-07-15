# Public API: Tw.Application

标识：Tw.Application / backend/dotnet/BuildingBlocks/src/Application/Tw.Application

公开能力边界：
- Tw.Application

实现公开命名空间：
- Tw.Application.Pipeline

公开类型：
- sealed class ApplicationPipelineExecutor - Tw.Application.Pipeline (backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ApplicationPipelineExecutor.cs:8)
- static class ApplicationPipelineServiceCollectionExtensions - Tw.Application.Pipeline (backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ApplicationPipelineServiceCollectionExtensions.cs:10)
- interface IApplicationPipelineBehavior - Tw.Application.Pipeline (backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/IApplicationPipelineBehavior.cs:6)
- interface ICompletedHook - Tw.Application.Pipeline (backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/ICompletedHook.cs:6)
- sealed class MediatRApplicationPipelineBehavior - Tw.Application.Pipeline (backend/dotnet/BuildingBlocks/src/Application/Tw.Application/Pipeline/MediatRApplicationPipelineBehavior.cs:14)

DI 注册入口：
- Tw.Application.Pipeline.ApplicationPipelineServiceCollectionExtensions.AddApplicationPipeline

包参考文档：
- docs/shared-packages/dotnet/Tw.Application/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Application
