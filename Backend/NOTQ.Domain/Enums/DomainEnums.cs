namespace NOTQ.Domain.Enums;

public enum UserRole
{
    Parent = 1,
    Therapist = 2,
    Admin = 3
}

public enum SessionStatus
{
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum PronunciationPrediction
{
    Correct = 1,
    Incorrect = 2
}

public enum IssueType
{
    None = 0,
    Substitution = 1,
    Omission = 2,
    Distortion = 3,
    Unknown = 4
}

public enum SessionTrend
{
    Improving = 1,
    Stable = 2,
    Declining = 3,
    InsufficientData = 4
}

public enum RecommendationLevel
{
    StandardPractice = 1,
    MonitoredPractice = 2,
    ProfessionalEvaluation = 3
}
