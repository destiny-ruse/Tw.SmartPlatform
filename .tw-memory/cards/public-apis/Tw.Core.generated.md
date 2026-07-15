# Public API: Tw.Core

标识：Tw.Core / backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core

公开能力边界：
- Tw.Check
- Tw.Collections
- Tw.Core.Primitives
- Tw.Reflection
- Tw.Exceptions
- Tw.Extensions
- Tw.Utilities
- Tw.Async

实现公开命名空间：
- Tw
- Tw.Collections
- Tw.Core.Primitives
- Tw.Exceptions
- Tw.Extensions
- Tw.Reflection

公开类型：
- static class Check - Tw (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Check.cs:8)
- interface ITypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Collections/ITypeList.cs:6)
- interface ITypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Collections/ITypeList.cs:12)
- class TypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Collections/TypeList.cs:8)
- class TypeList - Tw.Collections (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Collections/TypeList.cs:14)
- class NamedAction - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedAction.cs:11)
- class NamedActionList - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedActionList.cs:7)
- class NamedObject - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedObject.cs:9)
- class NamedObjectList - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedObjectList.cs:7)
- class NamedTypeSelector - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedTypeSelector.cs:10)
- static class NamedTypeSelectorListExtensions - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedTypeSelectorListExtensions.cs:6)
- class NamedValue - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedValue.cs:11)
- class NamedValue - Tw.Core.Primitives (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Primitives/NamedValue.cs:22)
- class TwException - Tw.Exceptions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Exceptions/TwException.cs:6)
- static class ByteArrayExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/ByteArrayExtensions.cs:8)
- static class CollectionExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/CollectionExtensions.cs:6)
- static class ComparableExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/ComparableExtensions.cs:6)
- static class DateTimeExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/DateTimeExtensions.cs:6)
- static class DictionaryExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/DictionaryExtensions.cs:9)
- static class ExceptionExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/ExceptionExtensions.cs:8)
- static class GuidExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/GuidExtensions.cs:6)
- static class NumberExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/NumberExtensions.cs:8)
- static class ObjectExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/ObjectExtensions.cs:6)
- static class StreamExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/StreamExtensions.cs:8)
- static class StringExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/StringExtensions.cs:9)
- static class TypeExtensions - Tw.Extensions (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Extensions/TypeExtensions.cs:6)
- sealed record CacheStatistics - Tw.Reflection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Reflection/ReflectionCache.cs:242)
- interface ITypeFinder - Tw.Reflection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Reflection/ITypeFinder.cs:8)
- static class ReflectionCache - Tw.Reflection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Reflection/ReflectionCache.cs:9)
- sealed class TypeFinder - Tw.Reflection (backend/dotnet/BuildingBlocks/src/Foundation/Tw.Core/Reflection/TypeFinder.cs:8)

DI 注册入口：
- none

包参考文档：
- docs/shared-packages/dotnet/Tw.Core/async/async-disposal.md
- docs/shared-packages/dotnet/Tw.Core/README.md

契约关联：
- none

消费提示：
- 公开能力来自 package-charter.yaml
- 实现公开 API 来自当前包源码
- 关联文档来自 docs/shared-packages

source_refs:
- charter:package-charter:Tw.Core
