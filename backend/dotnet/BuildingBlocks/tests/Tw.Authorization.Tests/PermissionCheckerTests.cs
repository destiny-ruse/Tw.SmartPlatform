using AwesomeAssertions;
using Tw.Authorization;
using Tw.Authorization.Abstractions;
using Xunit;

namespace Tw.Authorization.Tests;

public sealed class PermissionCheckerTests
{
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

    private sealed class InMemoryGrantStore(IReadOnlySet<string> grants) : IGrantStore
    {
        public Task<bool> HasGrantAsync(AuthorizationContext context, CancellationToken cancellationToken)
        {
            var key = $"{context.SubjectId}:{context.TenantId}:{context.Permission}:{context.ResourceType}:{context.ResourceId}";
            return Task.FromResult(grants.Contains(key));
        }
    }

    private sealed class InMemoryPermissionGrantCache : IPermissionGrantCache
    {
        private readonly Dictionary<PermissionGrantCacheKey, bool> _values = new();

        public Task<bool?> GetAsync(PermissionGrantCacheKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.TryGetValue(key, out var allowed) ? allowed : (bool?)null);
        }

        public Task SetAsync(PermissionGrantCacheKey key, bool allowed, CancellationToken cancellationToken)
        {
            _values[key] = allowed;
            return Task.CompletedTask;
        }
    }
}
