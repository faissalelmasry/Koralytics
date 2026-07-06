using Koralytics.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Middlewares;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogException(exception);

        var statusCode = exception is BaseBusinessException businessException
            ? (int)businessException.StatusCode
            : StatusCodes.Status500InternalServerError;

        var title = exception is BaseBusinessException businessExceptionTitle
            ? businessExceptionTitle.Title
            : "Internal Server Error";

        var detail = exception is BaseBusinessException
            ? exception.Message
            : "An unexpected error occurred.";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private void LogException(Exception exception)
    {
        switch (exception)
        {
            case BaseBusinessException:
                logger.LogWarning(exception, exception.Message);
                break;

            default:
                logger.LogError(exception, "An unexpected exception occurred.");
                break;
        }
    }
}