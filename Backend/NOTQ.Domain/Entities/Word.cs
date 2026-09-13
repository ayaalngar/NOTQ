using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace NOTQ.Domain.Entities;

public class Word
{
    public int Id { get; set; }

    [Column("Word")]
    [JsonPropertyName("word")]
    public string WordText { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; // "Letter" or "Word"
    public string ImageUrl { get; set; } = string.Empty;
    public string? CategoryLetter { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
