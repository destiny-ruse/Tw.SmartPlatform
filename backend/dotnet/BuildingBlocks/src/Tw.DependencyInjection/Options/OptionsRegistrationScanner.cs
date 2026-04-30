using System.Reflection;
using Tw.Core.Configuration;
using Tw.Core.Exceptions;

namespace Tw.DependencyInjection.Options;

/// <summary>
/// 从指定程序集集合中扫描所有选项候选类型，产出 <see cref="OptionsRegistrationDescriptor"/> 列表。
/// </summary>
public static class OptionsRegistrationScanner
{
    private static readonly string[] OptionsSuffixes = ["Options", "Settings"];

    /// <summary>
    /// 扫描程序集并产出选项注册描述符列表。
    /// </summary>
    /// <param name="libraryAssemblies">以类库约定扫描的程序集集合（必须有 <see cref="IConfigurableOptions"/> 实现或 <see cref="ConfigurationSectionAttribute"/> 标注）。</param>
    /// <param name="entryAssembly">以入口约定扫描的程序集（额外允许类型名后缀为 <c>Options</c> 或 <c>Settings</c> 的类型）。</param>
    /// <returns>按发现顺序排列、通过重复性验证的描述符列表。</returns>
    /// <exception cref="TwConfigurationException">
    /// 同一类型上存在重复的 <c>(OptionsType, OptionsName)</c> 组合时抛出。
    /// </exception>
    public static IReadOnlyList<OptionsRegistrationDescriptor> Scan(
        IEnumerable<Assembly> libraryAssemblies,
        Assembly? entryAssembly = null)
    {
        var descriptors = new List<OptionsRegistrationDescriptor>();

        foreach (var assembly in libraryAssemblies)
        {
            ScanAssembly(assembly, isEntryMode: false, descriptors);
        }

        if (entryAssembly is not null)
        {
            ScanAssembly(entryAssembly, isEntryMode: true, descriptors);
        }

        ValidateDuplicates(descriptors);

        return descriptors;
    }

    /// <summary>
    /// 扫描单个类型并返回描述符列表（不执行跨类型重复性验证）。供测试和调试按需调用。
    /// </summary>
    /// <param name="type">要扫描的类型。</param>
    /// <param name="isEntryMode">是否以入口模式扫描（允许后缀约定）。</param>
    /// <returns>零个或多个描述符（有多少 <see cref="ConfigurationSectionAttribute"/> 就产出多少）。</returns>
    public static IReadOnlyList<OptionsRegistrationDescriptor> ScanType(Type type, bool isEntryMode = false)
    {
        if (!IsCandidate(type, isEntryMode))
        {
            return [];
        }

        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        var attrs = type.GetCustomAttributes<ConfigurationSectionAttribute>(inherit: true).ToArray();
        var results = new List<OptionsRegistrationDescriptor>();

        if (attrs.Length > 0)
        {
            foreach (var attr in attrs)
            {
                results.Add(new OptionsRegistrationDescriptor(
                    OptionsType: type,
                    SectionName: attr.Name,
                    OptionsName: attr.OptionsName,
                    ValidateOnStart: attr.ValidateOnStart,
                    DirectInject: attr.DirectInject,
                    AssemblyName: assemblyName));
            }
        }
        else
        {
            var defaultSection = ComputeDefaultSectionName(type);
            results.Add(new OptionsRegistrationDescriptor(
                OptionsType: type,
                SectionName: defaultSection,
                OptionsName: null,
                ValidateOnStart: null,
                DirectInject: false,
                AssemblyName: assemblyName));
        }

        return results;
    }

    // ---- private helpers ----

    private static void ScanAssembly(Assembly assembly, bool isEntryMode, List<OptionsRegistrationDescriptor> results)
    {
        IEnumerable<Type?> rawTypes;
        try
        {
            rawTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            rawTypes = ex.Types.Where(t => t is not null);
        }

        foreach (var type in rawTypes.OfType<Type>())
        {
            if (!IsCandidate(type, isEntryMode))
            {
                continue;
            }

            var attrs = type.GetCustomAttributes<ConfigurationSectionAttribute>(inherit: true).ToArray();

            if (attrs.Length > 0)
            {
                // 每个 [ConfigurationSection] 属性产出一个描述符
                foreach (var attr in attrs)
                {
                    results.Add(new OptionsRegistrationDescriptor(
                        OptionsType: type,
                        SectionName: attr.Name,
                        OptionsName: attr.OptionsName,
                        ValidateOnStart: attr.ValidateOnStart,
                        DirectInject: attr.DirectInject,
                        AssemblyName: assembly.GetName().Name ?? string.Empty));
                }
            }
            else
            {
                // 无显式属性时（仅入口模式下基于后缀或 IConfigurableOptions 发现），使用默认节名称
                var defaultSection = ComputeDefaultSectionName(type);
                results.Add(new OptionsRegistrationDescriptor(
                    OptionsType: type,
                    SectionName: defaultSection,
                    OptionsName: null,
                    ValidateOnStart: null,
                    DirectInject: false,
                    AssemblyName: assembly.GetName().Name ?? string.Empty));
            }
        }
    }

    /// <summary>
    /// 判断类型是否为合法的选项候选。
    /// </summary>
    private static bool IsCandidate(Type type, bool isEntryMode)
    {
        // 必须是具体类
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        // 必须有无参构造函数（ConfigurationBinder 绑定的前提）
        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            return false;
        }

        // 必须有至少一个 public set/init 属性（绑定有意义的前提）
        if (!HasBindableProperty(type))
        {
            return false;
        }

        // 类库约定：必须有 IConfigurableOptions 或 [ConfigurationSection]
        // 入口约定：额外允许后缀为 Options/Settings
        var hasMarker = typeof(IConfigurableOptions).IsAssignableFrom(type);
        var hasAttribute = type.IsDefined(typeof(ConfigurationSectionAttribute), inherit: true);
        var hasSuffix = isEntryMode && HasOptionsSuffix(type.Name);

        return hasMarker || hasAttribute || hasSuffix;
    }

    private static bool HasBindableProperty(Type type)
    {
        return type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Any(p => p.CanWrite || IsInitOnlyProperty(p));
    }

    private static bool IsInitOnlyProperty(System.Reflection.PropertyInfo property)
    {
        var setter = property.SetMethod;
        if (setter is null) return false;
        return setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
    }

    private static bool HasOptionsSuffix(string typeName)
    {
        foreach (var suffix in OptionsSuffixes)
        {
            if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 将类型名去掉 <c>Options</c> 或 <c>Settings</c> 后缀作为默认节名称。
    /// 若无可识别的后缀，则直接使用类型名。
    /// </summary>
    private static string ComputeDefaultSectionName(Type type)
    {
        var name = type.Name;
        foreach (var suffix in OptionsSuffixes)
        {
            if (name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length)
            {
                return name[..^suffix.Length];
            }
        }
        return name;
    }

    /// <summary>
    /// 验证不存在重复的 <c>(OptionsType, OptionsName)</c> 对。
    /// </summary>
    private static void ValidateDuplicates(IReadOnlyList<OptionsRegistrationDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(d => (d.OptionsType, d.OptionsName))
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
        {
            return;
        }

        var messages = duplicates.Select(g =>
            $"类型 {g.Key.OptionsType.FullName} 存在重复的 OptionsName 声明（OptionsName={g.Key.OptionsName ?? "(null)"})。");

        throw new TwConfigurationException(
            $"选项注册配置存在重复声明，启动失败。{Environment.NewLine}{string.Join(Environment.NewLine, messages)}");
    }
}
