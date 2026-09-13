namespace NOTQ.Application.Interfaces;

public class WordVerificationResult
{
    public string TranscribedText { get; set; } = string.Empty;
    public bool Matched { get; set; }
    public double MatchScore { get; set; }
    public bool IsServiceUnavailable { get; set; }
}

public interface IWordVerificationService
{
    Task<WordVerificationResult> VerifyWordAsync(
        Stream audioStream,
        string targetWord,
        CancellationToken cancellationToken = default);
}
