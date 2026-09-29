using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace BankAccSim.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception");
        else
            logger.LogWarning("Request failed with {Status}: {Message}", status, exception.Message);

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception ex)
    {
        if (ex is AppException app)
            return (app.StatusCode, app.Title, app.Message);

        var pg = ex as PostgresException ?? ex.InnerException as PostgresException;
        if (pg is { SqlState: PostgresErrorCodes.SerializationFailure })
            return (409, "Conflict", "The operation conflicted with a concurrent request. Please retry.");

        return (500, "Internal Server Error", "An unexpected error occurred.");
    }
}