using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 基于动态文本存储的本地化文本贡献者，实现 <see cref="ITextResourceContributor"/>。
/// 支持租户隔离与文化回退，数据来源为运行时可写入的 <see cref="IDynamicTextStore"/>
/// </summary>
public sealed class DynamicTextContributor : ITextResourceContributor
{
    private readonly IDynamicTextStore _store;

    /// <summary>
    /// 初始化 <see cref="DynamicTextContributor"/> 类的新实例
    /// </summary>
    /// <param name="store">动态文本存储</param>
    /// <param name="priority">该贡献者在查找管道中的优先级；数值越大越优先执行</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="store"/> 为 <see langword="null"/> 时抛出</exception>
    public DynamicTextContributor(IDynamicTextStore store, int priority)
    {
        _store = Check.NotNull(store);
        Priority = priority;
    }

    /// <inheritdoc />
    public int Priority { get; }

    /// <inheritdoc />
    public ValueTask<LocalizedText?> GetOrNullAsync(
        TextLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        // 直接委托 store；store 已内置租户+文化回退逻辑
        return _store.FindAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask FillAsync(
        TextFillRequest request,
        IDictionary<string, LocalizedText> texts,
        CancellationToken cancellationToken = default)
    {
        var list = await _store.GetListAsync(request, cancellationToken).ConfigureAwait(false);

        // 按候选文化从低优先级到高优先级（倒序索引）写入，使高优先级文化（写入更晚）覆盖低优先级
        for (var i = request.CandidateCultureNames.Count - 1; i >= 0; i--)
        {
            var culture = request.CandidateCultureNames[i];
            foreach (var item in list)
            {
                if (string.Equals(item.CultureName, culture, StringComparison.OrdinalIgnoreCase))
                {
                    texts[item.Name] = item;
                }
            }
        }
    }
}
