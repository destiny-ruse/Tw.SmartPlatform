using System.Reflection;

namespace Tw.DependencyInjection.Discovery;

/// <summary>
/// 候选程序集来源，供发现器过滤排序前取数
/// </summary>
internal interface IAssemblySource
{
    /// <summary>
    /// 返回参与发现的候选程序集
    /// </summary>
    IReadOnlyList<Assembly> GetCandidateAssemblies();
}
