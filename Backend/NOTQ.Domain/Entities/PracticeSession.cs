using NOTQ.Domain.Common;
using NOTQ.Domain.Enums;

namespace NOTQ.Domain.Entities;

public class PracticeSession : BaseEntity<Guid>
{
    public Guid ChildId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.InProgress;
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public double Score { get; set; }

    public Child Child { get; set; } = null!;
    public ICollection<AudioAttempt> AudioAttempts { get; set; } = new List<AudioAttempt>();
}
