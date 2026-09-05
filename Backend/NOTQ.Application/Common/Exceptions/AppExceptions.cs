namespace NOTQ.Application.Common.Exceptions;

public abstract class BaseApplicationException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected BaseApplicationException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class NotFoundException : BaseApplicationException
{
    public NotFoundException(string message = "Requested resource was not found.")
        : base("NOT_FOUND", message, 404)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName.ToUpperInvariant()}_NOT_FOUND", $"{resourceName} with identifier '{key}' was not found.", 404)
    {
    }
}

public class ValidationException : BaseApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors, string message = "One or more validation errors occurred.")
        : base("VALIDATION_ERROR", message, 400)
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.", 400)
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

public class UnauthorizedException : BaseApplicationException
{
    public UnauthorizedException(string message = "Authentication failed or token is invalid.")
        : base("UNAUTHORIZED", message, 401)
    {
    }
}

public class ForbiddenException : BaseApplicationException
{
    public ForbiddenException(string message = "You do not have permission to access this resource.")
        : base("FORBIDDEN", message, 403)
    {
    }
}

public class ConflictException : BaseApplicationException
{
    public ConflictException(string message = "A conflict occurred with the current state of the resource.")
        : base("CONFLICT", message, 409)
    {
    }
}

public class AiServiceUnavailableException : BaseApplicationException
{
    public AiServiceUnavailableException(string message = "Speech analysis is temporarily unavailable. Please try again.")
        : base("AI_SERVICE_UNAVAILABLE", message, 503)
    {
    }
}
