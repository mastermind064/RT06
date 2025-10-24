using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RTMultiTenant.Api.Entities;

public class EventRecord
{
    [Key]
    public long EventId { get; set; }
    public Guid RtId { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string EventPayload { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public Guid CausedByUserId { get; set; }
    public int AggregateVersion { get; set; }

    [ForeignKey(nameof(CausedByUserId))]
    public User CausedBy { get; set; } = default!;
    [ForeignKey(nameof(RtId))]
    public Rt Rt { get; set; } = default!;
}
