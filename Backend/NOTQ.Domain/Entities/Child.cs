namespace NOTQ.Domain.Entities;

public class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string AvatarId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}

