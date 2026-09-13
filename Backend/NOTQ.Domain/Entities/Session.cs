namespace NOTQ.Domain.Entities;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildId { get; set; }
    public string Type { get; set; } = string.Empty; // "Assessment" or "Practice"
    public string Status { get; set; } = "InProgress"; // "InProgress" or "Completed"
    public int TotalWords { get; set; }
    public int CorrectCount { get; set; }
    public int NeedsPracticeCount { get; set; }
    public double Score { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Child Child { get; set; } = null!;
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
