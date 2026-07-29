using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;

namespace LifeBalance.OrganizationSaaS.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ITenantContext _tenantContext;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ITenantContext tenantContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Processing Request: {Name} | TenantId: {TenantId} | UserId: {UserId}",
            requestName, _tenantContext.TenantId, _tenantContext.UserId);

        var response = await next();

        _logger.LogInformation("Completed Request: {Name}", requestName);
        return response;
    }
}

public class MultiTenantValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantContext _tenantContext;

    public MultiTenantValidationBehavior(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_tenantContext.IsAuthenticated && string.IsNullOrWhiteSpace(_tenantContext.TenantId))
        {
            throw new MultiTenantViolationException("Authenticated user has no valid TenantId context.");
        }

        return await next();
    }
}
