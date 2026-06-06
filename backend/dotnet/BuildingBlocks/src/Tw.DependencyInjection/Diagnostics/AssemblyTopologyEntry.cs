namespace Tw.DependencyInjection.Diagnostics;

/// <summary>
/// 程序集在依赖拓扑中的位置
/// </summary>
/// <param name="AssemblyName">程序集名</param>
/// <param name="Level">拓扑层级；在扫描范围内无依赖的程序集为 0，依赖方层级递增</param>
public sealed record AssemblyTopologyEntry(string AssemblyName, int Level);
