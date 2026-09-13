using System.Text.Json.Serialization;

namespace NOTQ.Application.DTOs.Mobile;

// 1. POST /api/v1/children
public class CreateChildMobileRequestDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}

public class CreateChildMobileResponseDto
{
    [JsonPropertyName("childId")]
    public Guid ChildId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}

public class AuthTokenDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

// 2. GET /api/v1/assessment/words
public class AssessmentWordDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;
}

// 3. POST /api/v1/assessment/sessions & POST /api/v1/practice/sessions
public class StartSessionRequestDto
{
    [JsonPropertyName("childId")]
    public Guid ChildId { get; set; }
}

public class StartPracticeSessionRequestDto
{
    [JsonPropertyName("childId")]
    public Guid ChildId { get; set; }

    [JsonPropertyName("categoryFilter")]
    public string? CategoryFilter { get; set; }
}

public class StartSessionResponseDto
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }
}

// 4. POST /api/v1/assessment/sessions/{sessionId}/attempts
public class SubmitAttemptResponseDto
{
    [JsonPropertyName("attemptId")]
    public Guid AttemptId { get; set; }

    [JsonPropertyName("wordId")]
    public int WordId { get; set; }

    [JsonPropertyName("prediction")]
    public string? Prediction { get; set; }

    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    [JsonPropertyName("issueType")]
    public string? IssueType { get; set; }

    [JsonPropertyName("detectedWord")]
    public string? DetectedWord { get; set; }

    [JsonPropertyName("feedback")]
    public AttemptFeedbackDto Feedback { get; set; } = new();
}

public class AttemptFeedbackDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "correct", "retry", "needsPractice"

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// 5. POST /api/v1/assessment/sessions/{sessionId}/complete
public class CompleteAssessmentResponseDto
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "completed";

    [JsonPropertyName("totalWords")]
    public int TotalWords { get; set; }

    [JsonPropertyName("correctAttempts")]
    public int CorrectAttempts { get; set; }

    [JsonPropertyName("incorrectAttempts")]
    public int IncorrectAttempts { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}

// 6. GET /api/v1/home?childId={childId}
public class HomeScreenResponseDto
{
    [JsonPropertyName("child")]
    public HomeChildDto Child { get; set; } = new();

    [JsonPropertyName("progress")]
    public HomeProgressDto Progress { get; set; } = new();

    [JsonPropertyName("journey")]
    public HomeJourneyDto Journey { get; set; } = new();
}

public class HomeChildDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("avatar")]
    public string Avatar { get; set; } = string.Empty;
}

public class HomeProgressDto
{
    [JsonPropertyName("completedWords")]
    public int CompletedWords { get; set; }

    [JsonPropertyName("totalWords")]
    public int TotalWords { get; set; }

    [JsonPropertyName("practiceWords")]
    public int PracticeWords { get; set; }
}

public class HomeJourneyDto
{
    [JsonPropertyName("suggestedAction")]
    public string SuggestedAction { get; set; } = string.Empty; // "startAssessment" or "continuePractice"

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

// 8. POST /api/v1/practice/sessions/{sessionId}/attempts
public class PracticeAttemptResponseDto
{
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("feedback")]
    public AttemptFeedbackDto Feedback { get; set; } = new();
}

// 9. POST /api/v1/practice/sessions/{sessionId}/complete
public class CompletePracticeResponseDto
{
    [JsonPropertyName("correctCount")]
    public int CorrectCount { get; set; }

    [JsonPropertyName("needsPracticeCount")]
    public int NeedsPracticeCount { get; set; }

    [JsonPropertyName("words")]
    public List<PracticeWordStatusDto> Words { get; set; } = new();
}

public class PracticeWordStatusDto
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // "correct" or "needsPractice"
}
