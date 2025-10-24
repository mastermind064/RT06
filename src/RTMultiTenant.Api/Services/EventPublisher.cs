using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RTMultiTenant.Api.Data;
using RTMultiTenant.Api.Entities;

namespace RTMultiTenant.Api.Services;

public class EventPublisher
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantProvider _tenantProvider;

    public EventPublisher(AppDbContext dbContext, ITenantProvider tenantProvider)
    {
        _dbContext = dbContext;
        _tenantProvider = tenantProvider;
    }

    public async Task<EventRecord> AppendAsync(string aggregateType, Guid aggregateId, string eventType, object payload,
        Guid causedByUserId, CancellationToken cancellationToken = default)
    {
        var rtId = _tenantProvider.GetRtId();
        var version = await _dbContext.EventStore
            .Where(e => e.AggregateId == aggregateId && e.RtId == rtId)
            .Select(e => e.AggregateVersion)
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken);

        var record = new EventRecord
        {
            RtId = rtId,
            AggregateId = aggregateId,
            AggregateType = aggregateType,
            EventType = eventType,
            EventPayload = JsonSerializer.Serialize(payload),
            OccurredAt = DateTime.UtcNow,
            CausedByUserId = causedByUserId,
            AggregateVersion = version + 1
        };

        _dbContext.EventStore.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }
}
