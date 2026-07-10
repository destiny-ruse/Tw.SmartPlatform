namespace Tw.DependencyInjection.Discovery;

/// <summary>
/// 拓扑排序输入：程序集名与其引用的程序集名
/// </summary>
internal sealed record AssemblyDescriptor(string Name, IReadOnlyList<string> ReferencedAssemblyNames);
