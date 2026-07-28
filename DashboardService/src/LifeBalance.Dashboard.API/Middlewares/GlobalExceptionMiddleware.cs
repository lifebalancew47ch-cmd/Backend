using LifeBalance.Dashboard.Application.Exceptions;
using LifeBalance.Dashboard.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace LifeBalance.Dashboard.API.Middlewares;

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions and converts them to RFC 7807 Problem Details responses.
/// Registered in <c>Program.cs</c> before all other middleware.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>Initializes a new instance of <see cref="GlobalExceptionMiddleware"/>.</summary>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <inheritdoc cref="RequestDelegate"/>
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
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.UnprocessableEntity,
                "Validation Failure",
                "One or more validation errors occurred.",
                (object?)validationEx.Errors),

            NotFoundException notFoundEx => (
                (int)HttpStatusCode.NotFound,
                "Resource Not Found",
                notFoundEx.Message,
                (object?)null),

            DomainException domainEx => (
                (int)HttpStatusCode.BadRequest,
                "Domain Rule Violation",
                domainEx.Message,
                (object?)null),

            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "Unauthorized",
                "Authentication is required to access this resource.",
                (object?)null),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.",
                (object?)null)
        };

        _logger.LogError(
            exception,
            "Unhandled exception: {ExceptionType} — {Message}",
            exception.GetType().Name,
            exception.Message);

        var problemDetails = new ProblemDetails
        {
            Type     = $"https://httpstatuses.com/{statusCode}",
            Title    = title,
            Status   = statusCode,
            Detail   = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
