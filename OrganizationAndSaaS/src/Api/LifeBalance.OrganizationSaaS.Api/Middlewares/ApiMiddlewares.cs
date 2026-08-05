using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;

namespace LifeBalance.OrganizationSaaS.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled Exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
        };

        switch (exception)
        {
            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = "Authentication is required.";
                break;

            case ValidationException valEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Detail = "One or more validation errors occurred.";
                problemDetails.Extensions["errors"] = valEx.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
                break;

            case ResourceNotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFoundEx.Message;
                break;

            case MultiTenantViolationException tenantEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                problemDetails.Status = (int)HttpStatusCode.Forbidden;
                problemDetails.Title = "Multi-Tenant Access Violation";
                problemDetails.Detail = tenantEx.Message;
                break;

            case LimitExceededException limitEx:
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                problemDetails.Status = (int)HttpStatusCode.UnprocessableEntity;
                problemDetails.Title = "SaaS Plan Limit Exceeded";
                problemDetails.Detail = limitEx.Message;
                break;

            case DomainException domainEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Domain Rule Violation";
                problemDetails.Detail = domainEx.Message;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred. Please contact system support.";
                break;
        }

        var json = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(json);
    }
}

public class SecurityHeadersAndCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeader = "X-Correlation-Id";

    public SecurityHeadersAndCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Correlation ID handling
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }
        context.Response.Headers[CorrelationHeader] = correlationId;

        // 2. OWASP Recommended Security Headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self';";
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}
