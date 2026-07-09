using AwesomeAssertions;
using Tw.Authorization;
using Tw.Authorization.Abstractions;
using Xunit;

namespace Tw.Authorization.Tests;

/// <summary>验证 PermissionCheckerTests 相关行为</summary>
public sealed class PermissionCheckerTests
{
    /// <summary>验证 CheckAsync_ReturnsDenied_WhenPermissionMissing 场景</summary>
    /// <returns>CheckAsync_ReturnsDenied_WhenPermissionMissing 的执行结果</returns>
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

    /// <summary>验证 InMemoryGrantStore 相关行为</summary>
    private sealed class InMemoryGrantStore(IReadOnlySet<string> grants) : IGrantStore
    {
        /// <summary>验证 HasGrantAsync 场景</summary>
        /// <param name="context">context 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>HasGrantAsync 的执行结果</returns>
        public Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken)
        {
            var key = $"{context.SubjectId}:{context.TenantId}:{context.Permission}:{context.ResourceType}:{context.ResourceId}";
            return Task.FromResult(grants.Contains(key));
        }
    }

    /// <summary>验证 InMemoryPermissionGrantCache 相关行为</summary>
    private sealed class InMemoryPermissionGrantCache : IPermissionGrantCache
    {
        /// <summary>表示 _values 字段</summary>
        private readonly Dictionary<PermissionGrantCacheKey, bool> _values = new();

        /// <summary>验证 GetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>GetAsync 的执行结果</returns>
        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.TryGetValue(key, out var allowed) ? allowed : (bool?)null);
        }

        /// <summary>验证 SetAsync 场景</summary>
        /// <param name="key">key 参数</param>
        /// <param name="allowed">allowed 参数</param>
        /// <param name="cancellationToken">cancellationToken 参数</param>
        /// <returns>SetAsync 的执行结果</returns>
        public Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken)
        {
            _values[key] = allowed;
            return Task.CompletedTask;
        }
    }
}
