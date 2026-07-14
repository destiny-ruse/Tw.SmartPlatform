using AwesomeAssertions;
using Tw.Domain.Auditing;
using Tw.Domain.Concurrency;
using Tw.Domain.SoftDelete;
using Xunit;

namespace Tw.Domain.Tests;

/// <summary>
/// 验证领域实体的提供程序无关标记契约
/// </summary>
public sealed class EntityContractTests
{
    /// <summary>
    /// 审计契约保存创建与更新主体及时间
    /// </summary>
    [Fact]
    public void AuditedEntity_ExposesCreationAndUpdateAuditFields()
    {
        IAuditedEntity entity = new ContractEntity
        {
            CreatedAt = DateTimeOffset.Parse("2026-07-14T08:00:00+08:00"),
            UpdatedAt = DateTimeOffset.Parse("2026-07-14T09:00:00+08:00"),
            CreatedBy = "creator",
            UpdatedBy = "updater"
        };

        entity.CreatedAt.Should().Be(DateTimeOffset.Parse("2026-07-14T08:00:00+08:00"));
        entity.UpdatedAt.Should().Be(DateTimeOffset.Parse("2026-07-14T09:00:00+08:00"));
        entity.CreatedBy.Should().Be("creator");
        entity.UpdatedBy.Should().Be("updater");
        typeof(IAuditedEntity).Namespace.Should().Be("Tw.Domain.Auditing");
    }

    /// <summary>
    /// 并发戳契约允许持久化适配器读写不透明标识
    /// </summary>
    [Fact]
    public void ConcurrencyEntity_ExposesWritableConcurrencyStamp()
    {
        IHasConcurrencyStamp entity = new ContractEntity { ConcurrencyStamp = "stamp-1" };

        entity.ConcurrencyStamp.Should().Be("stamp-1");
        typeof(IHasConcurrencyStamp).Namespace.Should().Be("Tw.Domain.Concurrency");
    }

    /// <summary>
    /// 版本戳契约允许持久化适配器读写单调版本值
    /// </summary>
    [Fact]
    public void VersionedEntity_ExposesWritableVersionStamp()
    {
        IHasVersionStamp entity = new ContractEntity { VersionStamp = 42 };

        entity.VersionStamp.Should().Be(42);
        typeof(IHasVersionStamp).Namespace.Should().Be("Tw.Domain.Concurrency");
    }

    /// <summary>
    /// 软删除契约保存删除标记、主体和时间
    /// </summary>
    [Fact]
    public void SoftDeleteEntity_ExposesDeletionMarkers()
    {
        ISoftDelete entity = new ContractEntity
        {
            IsDeleted = true,
            DeletedAt = DateTimeOffset.Parse("2026-07-14T10:00:00+08:00"),
            DeletedBy = "deleter"
        };

        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(DateTimeOffset.Parse("2026-07-14T10:00:00+08:00"));
        entity.DeletedBy.Should().Be("deleter");
        typeof(ISoftDelete).Namespace.Should().Be("Tw.Domain.SoftDelete");
    }

    /// <summary>
    /// 为领域实体标记契约提供可读写的测试实体
    /// </summary>
    private sealed class ContractEntity : IAuditedEntity, IHasConcurrencyStamp, IHasVersionStamp, ISoftDelete
    {
        /// <summary>
        /// 实体创建时间
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// 实体最后更新时间
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// 创建实体的主体标识
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 最后更新实体的主体标识
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// 乐观并发不透明标识
        /// </summary>
        public string ConcurrencyStamp { get; set; } = string.Empty;

        /// <summary>
        /// 乐观并发数字版本
        /// </summary>
        public long VersionStamp { get; set; }

        /// <summary>
        /// 实体是否已逻辑删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 实体逻辑删除时间
        /// </summary>
        public DateTimeOffset? DeletedAt { get; set; }

        /// <summary>
        /// 执行逻辑删除的主体标识
        /// </summary>
        public string? DeletedBy { get; set; }
    }
}
