using AwesomeAssertions;
using Tw.EventBus.Cap.Storage;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Storage;

/// <summary>验证 SqlSugarCapStorageSchemaTests 相关行为</summary>
public sealed class SqlSugarCapStorageSchemaTests
{
    /// <summary>验证 DefaultSchema_UsesDedicatedCapTables 场景</summary>
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
