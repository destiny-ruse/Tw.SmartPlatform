using AwesomeAssertions;
using Tw.EventBus.Cap.Storage;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Storage;

/// <summary>
/// 覆盖SqlSugarCapStorage架构的核心行为和边界条件
/// </summary>
public sealed class SqlSugarCapStorageSchemaTests
{
    /// <summary>
    /// 验证默认架构UsesDedicatedCapTables
    /// </summary>
    [Fact]
    public void DefaultSchema_UsesDedicatedCapTables()
    {
        var schema = SqlSugarCapStorageSchema.FromOptions(new SqlSugarCapStorageOptions
        {
            ConnectionName = "Default"
        });

        schema.RequiredTables.Should().Equal("cap.published", "cap.received", "cap.locks");
        schema.IsTenantSharded.Should().BeFalse();
    }
}
