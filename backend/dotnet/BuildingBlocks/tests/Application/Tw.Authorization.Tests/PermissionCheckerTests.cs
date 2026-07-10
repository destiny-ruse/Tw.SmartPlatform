using AwesomeAssertions;
using Tw.Authorization;
using Tw.Authorization.Abstractions;
using Xunit;

namespace Tw.Authorization.Tests;

/// <summary>
/// 覆盖权限Checker的核心行为和边界条件
/// </summary>
public sealed class PermissionCheckerTests
{
    /// <summary>
    /// 验证Check异步返回拒绝当权限缺少
    /// </summary>
    /// <returns>表示异步流程完成状态的任务</returns>
    [Fact]
    public async Task CheckAsync_ReturnsDenied_WhenPermissionMissing()
    {
        var checker = new PermissionChecker(new InMemoryGrantStore(new HashSet<string>()), new InMemoryPermissionGrantCache());
        var context = new AuthorizationContext(
            SubjectId: "user-1",
            TenantId: "tenant-1",
            Permission: "orders.approve",
            ResourceType: "Order",
            ResourceId: "order-1",
            Roles: new HashSet<string>(StringComparer.Ordinal) { "cashier" });

        var result = await checker.CheckAsync(context, TestContext.Current.CancellationToken);

        result.Allowed.Should().BeFalse();
        result.Code.Should().Be("AUTHORIZATION:000001");
    }

    /// <summary>
    /// 覆盖InMemory授权记录存储的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryGrantStore(IReadOnlySet<string> grants) : IGrantStore
    {
        /// <summary>
        /// 判断测试授权存储中是否存在匹配授权记录
        /// </summary>
        /// <param name="context">当前调用携带的上下文信息</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的bool</returns>
        public Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken)
        {
            var key = $"{context.SubjectId}:{context.TenantId}:{context.Permission}:{context.ResourceType}:{context.ResourceId}";
            return Task.FromResult(grants.Contains(key));
        }
    }

    /// <summary>
    /// 覆盖InMemory权限授权记录缓存的核心行为和边界条件
    /// </summary>
    private sealed class InMemoryPermissionGrantCache : IPermissionGrantCache
    {
        /// <summary>
        /// 保存当前类型处理流程依赖的values
        /// </summary>
        private readonly Dictionary<PermissionGrantCacheKey, bool> _values = new();

        /// <summary>
        /// 从测试替身中读取指定条目
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>异步流程完成后产生的bool</returns>
        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.TryGetValue(key, out var allowed) ? allowed : (bool?)null);
        }

        /// <summary>
        /// 将指定条目写入测试替身
        /// </summary>
        /// <param name="key">用于定位目标数据或缓存项的键</param>
        /// <param name="allowed">用于提供allowed</param>
        /// <param name="cancellationToken">用于传播调用方取消请求的令牌</param>
        /// <returns>表示异步流程完成状态的任务</returns>
        public Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken)
        {
            _values[key] = allowed;
            return Task.CompletedTask;
        }
    }
}
