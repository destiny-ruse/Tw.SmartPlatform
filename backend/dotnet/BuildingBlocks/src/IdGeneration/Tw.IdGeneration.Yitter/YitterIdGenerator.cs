using Tw.IdGeneration;
using Yitter.IdGenerator;

namespace Tw.IdGeneration.Yitter;

/// <summary>
/// 基于 Yitter.IdGenerator 的长整型标识生成器
/// </summary>
public sealed class YitterIdGenerator : IIdGenerator
{
    /// <summary>初始化 YitterIdGenerator 实例</summary>
    private YitterIdGenerator()
    {
    }

    /// <summary>
    /// 使用指定 workerId 创建生成器
    /// </summary>
    /// <param name="workerId">当前节点的 workerId</param>
    /// <returns>Yitter 标识生成器</returns>
    /// <remarks>该方法会初始化 YitIdHelper 的进程级生成器配置</remarks>
    public static YitterIdGenerator CreateForWorker(ushort workerId)
    {
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions(workerId));
        return new YitterIdGenerator();
    }

    /// <inheritdoc />
    public long NewId()
    {
        return YitIdHelper.NextId();
    }
}
