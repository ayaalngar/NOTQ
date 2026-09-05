using NOTQ.Domain.Common;

namespace NOTQ.Domain.Entities;

public class PracticeWord : BaseEntity<int>
{
    public string Word { get; set; } = string.Empty;
    public string ExpectedPronunciation { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public string? TargetSound { get; set; }

    public ICollection<AudioAttempt> AudioAttempts { get; set; } = new List<AudioAttempt>();
}
