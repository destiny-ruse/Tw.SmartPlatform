using Tw.Context;
using Tw.Localization.Requests;

namespace Tw.Localization;

/// <summary>
/// 基于多贡献者管道的高层文本本地化服务，实现 <see cref="ITextLocalizer"/>。
/// 按贡献者优先级编排查找，并通过 <see cref="CultureFallback"/> 展开文化回退链
/// </summary>
public sealed class TextLocalizer : ITextLocalizer
{
    private readonly ITextResourceContributor[] _contributors;
    private readonly LocalizationOptions _options;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    /// <summary>
    /// 初始化 <see cref="TextLocalizer"/> 类的新实例
    /// </summary>
    /// <param name="contributors">本地化文本贡献者集合</param>
    /// <param name="options">系统级本地化配置</param>
    /// <param name="cancellationTokenProvider">取消令牌 provider</param>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="contributors"/>、<paramref name="options"/> 或 <paramref name="cancellationTokenProvider"/> 为 <see langword="null"/> 时抛出
    /// </exception>
    public TextLocalizer(
        IEnumerable<ITextResourceContributor> contributors,
        LocalizationOptions options,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _contributors = Check.NotNull(contributors).OrderBy(c => c.Priority).ToArray();
        _options = Check.NotNull(options);
        _cancellationTokenProvider = Check.NotNull(cancellationTokenProvider);
    }

    /// <inheritdoc />
    public async ValueTask<LocalizedText> GetAsync(
        string resourceName,
        string name,
        LocalizationContext context,
        CancellationToken cancellationToken = default)
    {
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);
        var candidates = CultureFallback.Expand(context, _options);
        var request = new TextLookupRequest(resourceName, name, context, candidates);

        // 按优先级降序遍历贡献者（数组已在构造时升序排列，倒向遍历即为降序），返回第一个非空结果
        for (var i = _contributors.Length - 1; i >= 0; i--)
        {
            var result = await _contributors[i].GetOrNullAsync(request, token).ConfigureAwait(false);
            if (result is not null)
            {
                return result;
            }
        }

        return LocalizedText.NotFound(resourceName, name, context.CultureName);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<LocalizedText>> GetAllAsync(
        string resourceName,
        LocalizationContext context,
        CancellationToken cancellationToken = default)
    {
        var token = _cancellationTokenProvider.FallbackToProvider(cancellationToken);
        var candidates = CultureFallback.Expand(context, _options);
        var request = new TextFillRequest(resourceName, context, candidates);
        var texts = new Dictionary<string, LocalizedText>(StringComparer.Ordinal);

        // 按优先级升序遍历贡献者（数组已在构造时排序），后执行的高优先级贡献者可覆盖低优先级贡献者的同名键
        foreach (var contributor in _contributors)
        {
            await contributor.FillAsync(request, texts, token).ConfigureAwait(false);
        }

        return texts.Values.ToList();
    }
}
