namespace EmployeeManagement.Api.Middleware;

// Thrown from services and turned into the right HTTP status by ExceptionMiddleware.
public class AppException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(StatusCodes.Status404NotFound, message) { }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message, IDictionary<string, string[]>? errors = null)
        : base(StatusCodes.Status400BadRequest, message, errors) { }

    public static BadRequestException ForField(string field, string message) =>
        new(message, new Dictionary<string, string[]> { [field] = new[] { message } });
}

public class ConflictException : AppException
{
    public ConflictException(string field, string message)
        : base(StatusCodes.Status409Conflict, message, new Dictionary<string, string[]> { [field] = new[] { message } }) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(StatusCodes.Status403Forbidden, message) { }
}
