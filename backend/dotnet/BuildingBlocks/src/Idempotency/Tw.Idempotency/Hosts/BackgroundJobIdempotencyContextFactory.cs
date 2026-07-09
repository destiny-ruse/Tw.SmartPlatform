namespace Tw.Idempotency.Hosts;

public static class BackgroundJobIdempotencyContextFactory
{
    public static IdempotencyKey Create(string tenantId, string jobName, string fireId)
    {
        HttpIdempotencyContextFactory.Validate(tenantId, jobName, fireId);
        return new IdempotencyKey(IdempotencyBoundary.BackgroundJob, tenantId, jobName, "Execute", fireId);
    }
}
