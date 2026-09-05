namespace NOTQ.Infrastructure.Storage;

public class AudioStorageOptions
{
    public const string SectionName = "AudioStorage";

    public string BaseFolder { get; set; } = "uploads/audio";
    public long MaxFileSizeBytes { get; set; } = 15 * 1024 * 1024; // 15MB
    public string[] AllowedExtensions { get; set; } = { ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".webm" };
}
