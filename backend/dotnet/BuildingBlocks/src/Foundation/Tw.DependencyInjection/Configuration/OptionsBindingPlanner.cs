using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Tw.DependencyInjection.Abstractions.Configuration;
using Tw.DependencyInjection.Diagnostics;

namespace Tw.DependencyInjection.Configuration;

/// <summary>
/// 根据已纳入扫描的程序集规划 Options 自动装载
/// </summary>
internal static class OptionsBindingPlanner
{
    /// <summary>
    /// 规划 Options 自动装载候选
    /// </summary>
    /// <param name="assemblies">已纳入扫描的程序集</param>
    /// <param name="typesByAssemblyName">按程序集名分组的类型集合</param>
    /// <param name="configuration">应用配置根</param>
    /// <returns>Options 绑定计划</returns>
    public static OptionsBindingPlan Plan(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, IReadOnlyList<Type>> typesByAssemblyName,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(typesByAssemblyName);
        ArgumentNullException.ThrowIfNull(configuration);

        var candidates = new List<OptionsBindingCandidate>();
        var diagnostics = new List<OptionsBindingDiagnostic>();
        var seenTypeAndName = new HashSet<(Type OptionsType, string Name)>();
        var seenSectionAndName = new Dictionary<SectionNameKey, Type>(SectionNameKeyComparer.Instance);

        foreach (var type in EnumerateTypes(assemblies, typesByAssemblyName))
        {
            if (!IsBindableOptionsType(type))
            {
                continue;
            }

            ValidateGenericContract(type);

            var sectionPath = ResolveSectionPath(type);
            var name = ResolveName(type);
            var validatorType = ResolveValidatorType(type);
            ValidateDuplicate(type, sectionPath, name, seenTypeAndName, seenSectionAndName);

            var sectionExists = configuration.GetSection(sectionPath).Exists();
            var isSensitive = IsSensitive(type);
            var candidate = new OptionsBindingCandidate(
                type,
                sectionPath,
                name,
                sectionExists,
                isSensitive,
                validatorType);
            candidates.Add(candidate);
            diagnostics.Add(new OptionsBindingDiagnostic(
                type.FullName ?? type.Name,
                sectionPath,
                name,
                sectionExists,
                sectionExists ? "bound" : "missing",
                "enabled",
                isSensitive));
        }

        return new OptionsBindingPlan(candidates, new OptionsBindingReport(diagnostics));
    }

    /// <summary>执行 EnumerateTypes 操作</summary>
    /// <param name="assemblies">assemblies 参数</param>
    /// <param name="typesByAssemblyName">typesByAssemblyName 参数</param>
    /// <returns>EnumerateTypes 的执行结果</returns>
    private static IEnumerable<Type> EnumerateTypes(
        IReadOnlyList<Assembly> assemblies,
        IReadOnlyDictionary<string, IReadOnlyList<Type>> typesByAssemblyName)
    {
        foreach (var assembly in assemblies)
        {
            var assemblyName = assembly.GetName().Name;
            if (assemblyName is not null &&
                typesByAssemblyName.TryGetValue(assemblyName, out var types))
            {
                foreach (var type in types)
                {
                    yield return type;
                }
            }
        }
    }

    /// <summary>执行 IsBindableOptionsType 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>IsBindableOptionsType 的执行结果</returns>
    private static bool IsBindableOptionsType(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        !type.IsGenericTypeDefinition &&
        type.GetConstructor(Type.EmptyTypes) is not null &&
        typeof(IConfigurableOptions).IsAssignableFrom(type) &&
        type.GetCustomAttribute<DisableOptionsBindingAttribute>() is null;

    /// <summary>执行 ValidateGenericContract 操作</summary>
    /// <param name="type">type 参数</param>
    private static void ValidateGenericContract(Type type)
    {
        foreach (var contract in type.GetInterfaces()
            .Where(@interface => @interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() == typeof(IConfigurableOptions<>)))
        {
            var optionsType = contract.GetGenericArguments()[0];
            if (optionsType != type)
            {
                throw new ServiceRegistrationException(
                    $"Options 类型 {type.FullName} 的 IConfigurableOptions<TOptions> 泛型参数必须等于自身类型");
            }
        }
    }

    /// <summary>执行 ResolveSectionPath 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>ResolveSectionPath 的执行结果</returns>
    private static string ResolveSectionPath(Type type)
    {
        var explicitPath = type.GetCustomAttribute<OptionsSectionAttribute>()?.Path;
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        const string suffix = "Options";
        return type.Name.EndsWith(suffix, StringComparison.Ordinal) && type.Name.Length > suffix.Length
            ? type.Name[..^suffix.Length]
            : type.Name;
    }

    /// <summary>执行 ResolveName 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>ResolveName 的执行结果</returns>
    private static string ResolveName(Type type) =>
        type.GetCustomAttribute<OptionsNameAttribute>()?.Name ?? Options.DefaultName;

    /// <summary>执行 ResolveValidatorType 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>ResolveValidatorType 的执行结果</returns>
    private static Type? ResolveValidatorType(Type type)
    {
        var validatorType = type.GetCustomAttribute<Tw.DependencyInjection.Abstractions.Configuration.OptionsValidatorAttribute>()
            ?.ValidatorType;

        if (validatorType is null &&
            type.GetInterfaces().Any(@interface =>
                @interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() == typeof(IValidateOptions<>) &&
                @interface.GetGenericArguments()[0] == type))
        {
            validatorType = type;
        }

        if (validatorType is not null)
        {
            var expectedContract = typeof(IValidateOptions<>).MakeGenericType(type);
            if (!expectedContract.IsAssignableFrom(validatorType))
            {
                throw new ServiceRegistrationException(
                    $"Options 校验器 {validatorType.FullName} 必须实现 IValidateOptions<{type.Name}>");
            }
        }

        return validatorType;
    }

    /// <summary>执行 ValidateDuplicate 操作</summary>
    /// <param name="type">type 参数</param>
    /// <param name="sectionPath">sectionPath 参数</param>
    /// <param name="name">name 参数</param>
    /// <param name="seenTypeAndName">seenTypeAndName 参数</param>
    /// <param name="seenSectionAndName">seenSectionAndName 参数</param>
    private static void ValidateDuplicate(
        Type type,
        string sectionPath,
        string name,
        HashSet<(Type OptionsType, string Name)> seenTypeAndName,
        Dictionary<SectionNameKey, Type> seenSectionAndName)
    {
        var typeKey = (type, name);
        if (!seenTypeAndName.Add(typeKey))
        {
            throw new ServiceRegistrationException(
                $"Options 类型 {type.FullName} 的命名实例 {name} 重复");
        }

        var sectionKey = new SectionNameKey(sectionPath, name);
        if (seenSectionAndName.TryGetValue(sectionKey, out var existingType))
        {
            throw new ServiceRegistrationException(
                $"Options 配置路径重复: {sectionPath} / {name} 被 {existingType.FullName} 与 {type.FullName} 同时使用");
        }

        seenSectionAndName[sectionKey] = type;
    }

    /// <summary>执行 IsSensitive 操作</summary>
    /// <param name="type">type 参数</param>
    /// <returns>IsSensitive 的执行结果</returns>
    private static bool IsSensitive(Type type) =>
        type.GetCustomAttribute<SensitiveConfigurationAttribute>() is not null ||
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.GetCustomAttribute<SensitiveConfigurationAttribute>() is not null);

    /// <summary>表示 SectionNameKey 声明</summary>
    private readonly record struct SectionNameKey(string SectionPath, string Name);

    /// <summary>表示 SectionNameKeyComparer 类型</summary>
    private sealed class SectionNameKeyComparer : IEqualityComparer<SectionNameKey>
    {
        /// <summary>表示 Instance 字段</summary>
        public static readonly SectionNameKeyComparer Instance = new();

        /// <summary>执行 Equals 操作</summary>
        /// <param name="x">x 参数</param>
        /// <param name="y">y 参数</param>
        /// <returns>Equals 的执行结果</returns>
        public bool Equals(SectionNameKey x, SectionNameKey y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.SectionPath, y.SectionPath) &&
            StringComparer.Ordinal.Equals(x.Name, y.Name);

        /// <summary>执行 GetHashCode 操作</summary>
        /// <param name="obj">obj 参数</param>
        /// <returns>GetHashCode 的执行结果</returns>
        public int GetHashCode(SectionNameKey obj)
        {
            var hash = new HashCode();
            hash.Add(obj.SectionPath, StringComparer.OrdinalIgnoreCase);
            hash.Add(obj.Name, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }
}
