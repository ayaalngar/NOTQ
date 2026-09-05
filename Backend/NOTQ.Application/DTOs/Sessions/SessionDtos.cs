using NOTQ.Domain.Enums;

namespace NOTQ.Application.DTOs.Sessions;

public class StartSessionDto
{
    public Guid ChildId { get; set; }
}

public class SessionResponseDto
{
    public Guid SessionId { get; set; }
    public Guid ChildId { get; set; }
    public string? ChildName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public SessionStatus Status { get; set; }
    public int TotalAttempts { get; set; }
    public int CorrectAttempts { get; set; }
    public double Score { get; set; }
}

public class CompleteSessionResponseDto
{
    public Guid SessionId { get; set; }
    public SessionStatus Status { get; set; }
    public int TotalWords { get; set; }
    public int CorrectAttempts { get; set; }
    public int IncorrectAttempts { get; set; }
    public double Score { get; set; }
}
