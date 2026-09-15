using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NOTQ.Application.Interfaces;

namespace NOTQ.Infrastructure.AI;

public class GradioOptions
{
    public const string SectionName = "GradioTranscriptionApi";
    public string BaseUrl { get; set; } = "https://bas77sel-notq-api.hf.space";
    public int TimeoutSeconds { get; set; } = 30;
    public string? ApiToken { get; set; }
}

public class GradioWordVerificationService : IWordVerificationService
{
    public const string ClientName = "NotqGradioTranscriptionApi";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GradioWordVerificationService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public GradioWordVerificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<GradioWordVerificationService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(ClientName);
        _logger = logger;
    }

    public async Task<WordVerificationResult> VerifyWordAsync(
        Stream audioStream,
        string targetWord,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GradioTranscription: Starting verification for target word '{TargetWord}'", targetWord);

            // Step 1: Upload audio file to /gradio_api/upload
            using var uploadContent = new MultipartFormDataContent();
            using var fileContent = new StreamContent(audioStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            uploadContent.Add(fileContent, "files", "attempt.wav");

            var uploadResponse = await _httpClient.PostAsync("gradio_api/upload", uploadContent, cancellationToken);
            var uploadResponseBody = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!uploadResponse.IsSuccessStatusCode)
            {
                var knownErr = CheckForKnownErrors(uploadResponseBody, (int)uploadResponse.StatusCode);
                _logger.LogWarning("GradioTranscription: Upload failed with status {StatusCode}: {Body}", uploadResponse.StatusCode, uploadResponseBody);
                return FailureResult(knownErr ?? $"Upload failed with status {(int)uploadResponse.StatusCode} ({uploadResponse.StatusCode})");
            }

            var uploadedPaths = JsonSerializer.Deserialize<List<string>>(uploadResponseBody);
            if (uploadedPaths == null || uploadedPaths.Count == 0)
            {
                _logger.LogWarning("GradioTranscription: Upload response contained no paths: {Body}", uploadResponseBody);
                return FailureResult("Upload response contained no valid paths");
            }

            var remoteFilePath = uploadedPaths[0];
            _logger.LogInformation("GradioTranscription: Upload succeeded. Remote path: {RemotePath}", remoteFilePath);

            // Step 2: Queue transcription request via POST /gradio_api/call/v2/transcribe
            var callPayload = new
            {
                audio_path = new
                {
                    path = remoteFilePath,
                    meta = new { _type = "gradio.FileData" }
                }
            };

            var callJson = JsonSerializer.Serialize(callPayload);
            using var callContent = new StringContent(callJson, Encoding.UTF8, "application/json");

            var callResponse = await _httpClient.PostAsync("gradio_api/call/v2/transcribe", callContent, cancellationToken);
            var callResponseBody = await callResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!callResponse.IsSuccessStatusCode)
            {
                var knownErr = CheckForKnownErrors(callResponseBody, (int)callResponse.StatusCode);
                _logger.LogWarning("GradioTranscription: Call transcribe failed with status {StatusCode}: {Body}", callResponse.StatusCode, callResponseBody);
                return FailureResult(knownErr ?? $"Call transcribe failed with status {(int)callResponse.StatusCode} ({callResponse.StatusCode})");
            }

            using var callDoc = JsonDocument.Parse(callResponseBody);
            if (!callDoc.RootElement.TryGetProperty("event_id", out var eventIdElement))
            {
                _logger.LogWarning("GradioTranscription: No event_id in call response: {Body}", callResponseBody);
                return FailureResult("No event_id in transcribe call response");
            }

            var eventId = eventIdElement.GetString();
            if (string.IsNullOrEmpty(eventId))
            {
                _logger.LogWarning("GradioTranscription: Empty event_id in call response");
                return FailureResult("Empty event_id in call response");
            }

            _logger.LogInformation("GradioTranscription: Transcription job started. EventId={EventId}", eventId);

            // Step 3: Stream SSE results from GET /gradio_api/call/transcribe/{eventId}
            var sseRequest = new HttpRequestMessage(HttpMethod.Get, $"gradio_api/call/transcribe/{eventId}");
            sseRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var sseResponse = await _httpClient.SendAsync(sseRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!sseResponse.IsSuccessStatusCode)
            {
                var sseErrorBody = await sseResponse.Content.ReadAsStringAsync(cancellationToken);
                var knownErr = CheckForKnownErrors(sseErrorBody, (int)sseResponse.StatusCode);
                _logger.LogWarning("GradioTranscription: SSE stream connection failed with status {StatusCode}: {Body}", sseResponse.StatusCode, sseErrorBody);
                return FailureResult(knownErr ?? $"SSE stream connection failed with status {(int)sseResponse.StatusCode} ({sseResponse.StatusCode})");
            }

            using var stream = await sseResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? transcribedText = null;
            string? eventErrorMessage = null;
            bool isCompleteEvent = false;
            bool isErrorEvent = false;

            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                line = line.Trim();
                if (line.StartsWith("event:"))
                {
                    var eventName = line.Substring(6).Trim();
                    isCompleteEvent = string.Equals(eventName, "complete", StringComparison.OrdinalIgnoreCase);
                    isErrorEvent = string.Equals(eventName, "error", StringComparison.OrdinalIgnoreCase);
                }
                else if (line.StartsWith("data:"))
                {
                    var dataPayload = line.Substring(5).Trim();
                    if (isErrorEvent)
                    {
                        var knownErr = CheckForKnownErrors(dataPayload);
                        eventErrorMessage = knownErr ?? ExtractErrorMessage(dataPayload);
                        _logger.LogWarning("GradioTranscription: Gradio Space returned an error event: {ErrorMessage}", eventErrorMessage);
                        break;
                    }
                    if (isCompleteEvent)
                    {
                        try
                        {
                            using var dataDoc = JsonDocument.Parse(dataPayload);
                            if (dataDoc.RootElement.ValueKind == JsonValueKind.Array && dataDoc.RootElement.GetArrayLength() > 0)
                            {
                                transcribedText = dataDoc.RootElement[0].GetString();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "GradioTranscription: Failed parsing complete event data payload: {DataPayload}", dataPayload);
                        }
                        break;
                    }
                }
            }

            if (eventErrorMessage != null)
            {
                _logger.LogWarning("GradioTranscription: Transcription failed: {ErrorMessage}", eventErrorMessage);
                return FailureResult(eventErrorMessage);
            }

            if (transcribedText == null)
            {
                _logger.LogWarning("GradioTranscription: Transcription failed. No completed transcribed text received");
                return FailureResult("No completed transcribed text received");
            }

            transcribedText = transcribedText.Trim();
            _logger.LogInformation("GradioTranscription: Transcription completed. Transcribed='{Transcribed}'", transcribedText);

            var score = CalculateMatchScore(targetWord, transcribedText);
            var isMatched = score >= 0.70;

            _logger.LogInformation(
                "GradioTranscription: Verification result. Target='{Target}', Transcribed='{Transcribed}', Score={Score:F2}, Matched={Matched}",
                targetWord, transcribedText, score, isMatched);

            return new WordVerificationResult
            {
                TranscribedText = transcribedText,
                Matched = isMatched,
                MatchScore = score,
                IsServiceUnavailable = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GradioTranscription: Exception during verification for word '{TargetWord}'", targetWord);
            return FailureResult($"Exception: {ex.Message}");
        }
    }

    private string? CheckForKnownErrors(string? content, int? statusCode = null)
    {
        if (string.IsNullOrWhiteSpace(content) && statusCode != 429) return null;

        if (statusCode == 429 || (content != null && (
            content.Contains("ZeroGPU quota exceeded", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase))))
        {
            _logger.LogWarning("GradioTranscription: ZeroGPU quota exceeded. Authenticate with a Hugging Face token (HF_TOKEN) for more quota.");
            return "ZeroGPU quota exceeded: Authenticate with a Hugging Face token for more quota";
        }

        if (content != null && (
            content.Contains("Parameter `data` is not a valid key-word argument", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("Parameter data is not a valid key-word argument", StringComparison.OrdinalIgnoreCase) ||
            (content.Contains("Field required", StringComparison.OrdinalIgnoreCase) && content.Contains("data", StringComparison.OrdinalIgnoreCase))))
        {
            _logger.LogWarning("GradioTranscription: Invalid Gradio request payload contract.");
            return "Invalid Gradio request: Parameter data is not a valid key-word argument";
        }

        return null;
    }

    private static string ExtractErrorMessage(string dataPayload)
    {
        try
        {
            using var doc = JsonDocument.Parse(dataPayload);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
                {
                    return errProp.GetString() ?? dataPayload;
                }
                if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                {
                    return msgProp.GetString() ?? dataPayload;
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString() ?? dataPayload;
            }
        }
        catch
        {
            // Not JSON, return raw payload
        }

        return string.IsNullOrWhiteSpace(dataPayload) ? "Gradio error event received" : dataPayload;
    }

    private static WordVerificationResult FailureResult(string errorMessage)
    {
        return new WordVerificationResult
        {
            TranscribedText = string.Empty,
            Matched = false,
            MatchScore = 0,
            IsServiceUnavailable = true,
            ErrorMessage = errorMessage
        };
    }

    public static double CalculateMatchScore(string target, string transcribed)
    {
        var normTarget = NormalizeArabic(target);
        var normTranscribed = NormalizeArabic(transcribed);

        if (string.Equals(normTarget, normTranscribed, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        if (normTranscribed.Contains(normTarget, StringComparison.OrdinalIgnoreCase) ||
            normTarget.Contains(normTranscribed, StringComparison.OrdinalIgnoreCase))
        {
            return 0.85;
        }

        var distance = ComputeLevenshteinDistance(normTarget, normTranscribed);
        var maxLength = Math.Max(normTarget.Length, normTranscribed.Length);
        if (maxLength == 0) return 1.0;

        return Math.Max(0.0, 1.0 - ((double)distance / maxLength));
    }

    private static string NormalizeArabic(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Remove diacritics / Tashkeel
        var result = Regex.Replace(text, @"[\u064B-\u065F\u0670]", "");
        // Remove Tatweel (kashida)
        result = Regex.Replace(result, @"\u0640", "");
        // Normalize Alefs (أ, إ, آ -> ا)
        result = Regex.Replace(result, @"[أإآٱ]", "ا");
        // Normalize Taa Marbuta (ة -> ه)
        result = Regex.Replace(result, @"ة", "ه");
        // Normalize Yaa (ى -> ي)
        result = Regex.Replace(result, @"ى", "ي");

        return result.Trim();
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; d[i, 0] = i++) { }
        for (var j = 0; j <= m; d[0, j] = j++) { }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
