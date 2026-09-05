using System.Net;
using System.Text.Json;
using NOTQ.Application.Common.Exceptions;
using NOTQ.Application.Common.Models;

namespace NOTQ.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse apiResponse;

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse = ApiResponse.Fail("VALIDATION_ERROR", validationEx.Message, validationEx.Errors);
                _logger.LogWarning("Validation failure: {Message}", validationEx.Message);
                break;

            case UnauthorizedException unauthorizedEx:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse = ApiResponse.Fail("UNAUTHORIZED", unauthorizedEx.Message);
                _logger.LogWarning("Unauthorized access attempt: {Message}", unauthorizedEx.Message);
                break;

            case ForbiddenException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                apiResponse = ApiResponse.Fail("FORBIDDEN", forbiddenEx.Message);
                _logger.LogWarning("Forbidden access attempt: {Message}", forbiddenEx.Message);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse = ApiResponse.Fail(notFoundEx.Code, notFoundEx.Message);
                _logger.LogWarning("Resource not found: {Message}", notFoundEx.Message);
                break;

            case ConflictException conflictEx:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                apiResponse = ApiResponse.Fail("CONFLICT", conflictEx.Message);
                _logger.LogWarning("Conflict occurred: {Message}", conflictEx.Message);
                break;

            case AiServiceUnavailableException aiEx:
                response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                apiResponse = ApiResponse.Fail("AI_SERVICE_UNAVAILABLE", aiEx.Message);
                _logger.LogError(exception, "Speech analysis service failure: {Message}", aiEx.Message);
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse = ApiResponse.Fail("INTERNAL_SERVER_ERROR", "An unexpected error occurred. Please try again later.");
                _logger.LogError(exception, "Unhandled exception occurred while processing request to {Path}", context.Request.Path);
                break;
        }

        var json = JsonSerializer.Serialize(apiResponse, JsonOptions);
        await response.WriteAsync(json);
    }
}
