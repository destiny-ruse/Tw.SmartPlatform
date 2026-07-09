using AwesomeAssertions;
using Tw.EventBus.Cap.Storage;
using Xunit;

namespace Tw.EventBus.Cap.Tests.Storage;

public sealed class SqlSugarCapStorageSchemaTests
{
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
