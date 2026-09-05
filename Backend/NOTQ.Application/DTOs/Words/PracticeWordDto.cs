namespace NOTQ.Application.DTOs.Words;

public class PracticeWordDto
{
    public int Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public string ExpectedPronunciation { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string? TargetSound { get; set; }
}
