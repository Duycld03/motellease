using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Abstractions;
using MotelLease.Application.Common.Errors;

namespace MotelLease.Api.Errors;

/// <summary>
/// Turns an <see cref="AppException"/> into RFC 7807 problem+json in the request language.
/// Handlers throw resource keys; the sentence is chosen here (docs/api-design.md, Conventions).
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>
    /// The localizer and the request context are taken per request rather than through the
    /// constructor: middleware itself is a singleton and must not capture scoped services.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ILocalizer localizer,
        IRequestContext requestContext)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            // Expected outcomes of a request, not faults: logged at information so a wrong
            // password does not read like an incident.
            logger.LogInformation(
                "Request {Method} {Path} rejected with {Status} ({Key}).",
                context.Request.Method,
                context.Request.Path,
                exception.StatusCode,
                exception.MessageKey);

            if (exception is TooManyRequestsException throttled)
            {
                context.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(throttled.RetryAfter.TotalSeconds))
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            await WriteAsync(
                context,
                exception.StatusCode,
                localizer.Get(exception.MessageKey, requestContext.Language, exception.Arguments),
                exception.MessageKey);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client hung up. Nothing to report and nothing to write to.
            logger.LogDebug(
                "Request {Method} {Path} aborted by the client.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception on {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            // The message is a generic one on purpose: exception text can name tables,
            // connection strings or file paths.
            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                localizer.Get(MessageKeys.General.Unexpected, requestContext.Language),
                MessageKeys.General.Unexpected);
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int status,
        string detail,
        string messageKey)
    {
        if (context.Response.HasStarted)
        {
            // Too late to replace the body; the client will see a truncated response, which
            // is the best available outcome.
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.For(status),
            Detail = detail,
            Instance = context.Request.Path
        };

        // Clients switch on the key, users read the detail.
        problem.Extensions["code"] = messageKey;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, ProblemJson.Options),
            context.RequestAborted);
    }
}

internal static class ProblemJson
{
    /// <summary>
    /// Matches the camelCase the MVC pipeline uses, so a problem written here looks like one
    /// written by the framework.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal static class ReasonPhrases
{
    internal static string For(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        _ => "Internal Server Error"
    };
}
