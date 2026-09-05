using NOTQ.Domain.Common;

namespace NOTQ.Domain.Entities;

public class Child : BaseEntity<Guid>
{
    public Guid ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }

    public User Parent { get; set; } = null!;
    public ICollection<PracticeSession> PracticeSessions { get; set; } = new List<PracticeSession>();
}
