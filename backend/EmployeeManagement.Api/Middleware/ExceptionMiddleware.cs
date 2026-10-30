using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (AppException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // fallback in case two requests slip past the duplicate check at the same time
            _logger.LogWarning(ex, "Unique constraint violation");
            await WriteProblem(context, StatusCodes.Status409Conflict,
                "A record with the same email or employee code already exists.", null);
        }
        catch (Exception ex) when (IsDeadlock(ex))
        {
            _logger.LogWarning(ex, "Deadlock on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status409Conflict,
                "This employee was being changed by someone else at the same time. Please reload and try again.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Something went wrong on the server. Please try again.", null);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);

    // SQL Server error 1205 = chosen as deadlock victim (may be wrapped by EF)
    private static bool IsDeadlock(Exception ex) =>
        (ex as SqlException ?? ex.InnerException as SqlException)?.Number == 1205;

    private static Task WriteProblem(HttpContext context, int status, string message, IDictionary<string, string[]>? errors)
    {
        var problem = errors == null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);
        problem.Status = status;
        problem.Title = message;

        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync<object>(problem, options: null, contentType: "application/problem+json");
    }
}
