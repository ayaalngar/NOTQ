using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Interfaces;

namespace NOTQ.Infrastructure.Storage;

public class LocalAudioStorageService : IAudioStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly AudioStorageOptions _options;

    public LocalAudioStorageService(
        IWebHostEnvironment environment,
        IOptions<AudioStorageOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<string> SaveAudioAsync(
        Stream audioStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            extension = ".wav";
        }

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new ValidationException("Audio", $"Invalid file extension. Allowed extensions are: {string.Join(", ", _options.AllowedExtensions)}");
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine(_options.BaseFolder, now.ToString("yyyy"), now.ToString("MM"));
        var targetDir = Path.Combine(webRoot, relativeDir);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetDir, fileName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await audioStream.CopyToAsync(fileStream, cancellationToken);
        }

        // Return relative web URL (with forward slashes)
        var urlPath = "/" + Path.Combine(relativeDir, fileName).Replace('\\', '/');
        return urlPath;
    }

    public Task<bool> DeleteAudioAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var cleanPath = relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, cleanPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
        }
        catch
        {
            // Suppress failure during delete
        }

        return Task.FromResult(false);
    }
}
