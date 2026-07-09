namespace Tw.Auditing.Contracts;

/// <summary>定义 IAuditStore 契约</summary>
public interface IAuditStore
{
    /// <summary>执行 StoreAsync 操作</summary>
    /// <param name="auditEvent">auditEvent 参数</param>
    /// <param name="cancellationToken">cancellationToken 参数</param>
    /// <returns>StoreAsync 的执行结果</returns>
    Task StoreAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
