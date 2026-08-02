using FluentValidation;
using MediatR;
using LifeBalance.OrganizationSaaS.Application.Common.Models;
using LifeBalance.OrganizationSaaS.Application.Interfaces;
using LifeBalance.OrganizationSaaS.Domain.Entities;
using LifeBalance.OrganizationSaaS.Domain.Exceptions;
using LifeBalance.OrganizationSaaS.Domain.Interfaces;

namespace LifeBalance.OrganizationSaaS.Application.Features.LicensesAndSubscriptions;

// --- DTOs ---
public record LicenseDto(
    string Id,
    string OrganizationId,
    string TenantId,
    string LicenseKey,
    string Type,
    string Status,
    string? AssignedUserId,
    DateTime IssuedAt,
    DateTime ExpiresAt
);

public record SubscriptionDto(
    string Id,
    string OrganizationId,
    string TenantId,
    string PlanId,
    string Status,
    DateTime RenewalDate,
    string BillingCycle,
    List<string> PaymentHistoryLog
);

public record InvitationDto(
    string Id,
    string TargetEmail,
    string TenantId,
    string? OrganizationId,
    string? FamilyId,
    string Role,
    string Token,
    string Status,
    DateTime SentAt,
    DateTime ExpiresAt
);

// --- License Commands & Queries ---
public record CreateLicenseCommand(string OrganizationId, string Type, DateTime ExpiresAt) : IRequest<ApiResponse<LicenseDto>>;
public record AssignLicenseCommand(string LicenseId, string UserId) : IRequest<ApiResponse<bool>>;
public record RevokeLicenseCommand(string LicenseId) : IRequest<ApiResponse<bool>>;
public record RenewLicenseCommand(string LicenseId, DateTime NewExpiration) : IRequest<ApiResponse<bool>>;
public record GetLicenseByIdQuery(string Id) : IRequest<ApiResponse<LicenseDto>>;
public record GetLicensesPagedQuery(string OrganizationId, int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<LicenseDto>>>;

// --- Subscription Commands & Queries ---
public record CreateSubscriptionCommand(string OrganizationId, string PlanId, string BillingCycle = "Monthly") : IRequest<ApiResponse<SubscriptionDto>>;
public record RenewSubscriptionCommand(string SubscriptionId) : IRequest<ApiResponse<bool>>;
public record ChangeSubscriptionPlanCommand(string SubscriptionId, string NewPlanId) : IRequest<ApiResponse<bool>>;
public record GetSubscriptionByIdQuery(string Id) : IRequest<ApiResponse<SubscriptionDto>>;
public record GetSubscriptionsPagedQuery(int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<SubscriptionDto>>>;

// --- Invitation Commands & Queries ---
public record CreateInvitationCommand(string TargetEmail, string? OrganizationId = null, string? FamilyId = null, string Role = "Member") : IRequest<ApiResponse<InvitationDto>>;
public record AcceptInvitationCommand(string Token) : IRequest<ApiResponse<bool>>;
public record RejectInvitationCommand(string Token) : IRequest<ApiResponse<bool>>;
public record CancelInvitationCommand(string InvitationId) : IRequest<ApiResponse<bool>>;
public record ResendInvitationCommand(string InvitationId) : IRequest<ApiResponse<bool>>;
public record GetInvitationByIdQuery(string Id) : IRequest<ApiResponse<InvitationDto>>;
public record GetInvitationsPagedQuery(int PageIndex = 1, int PageSize = 10) : IRequest<ApiResponse<PagedResult<InvitationDto>>>;

// --- Handlers ---
public class LicenseAndSubscriptionCommandHandler :
    IRequestHandler<CreateLicenseCommand, ApiResponse<LicenseDto>>,
    IRequestHandler<AssignLicenseCommand, ApiResponse<bool>>,
    IRequestHandler<RevokeLicenseCommand, ApiResponse<bool>>,
    IRequestHandler<RenewLicenseCommand, ApiResponse<bool>>,
    IRequestHandler<CreateSubscriptionCommand, ApiResponse<SubscriptionDto>>,
    IRequestHandler<RenewSubscriptionCommand, ApiResponse<bool>>,
    IRequestHandler<ChangeSubscriptionPlanCommand, ApiResponse<bool>>,
    IRequestHandler<CreateInvitationCommand, ApiResponse<InvitationDto>>,
    IRequestHandler<AcceptInvitationCommand, ApiResponse<bool>>,
    IRequestHandler<RejectInvitationCommand, ApiResponse<bool>>,
    IRequestHandler<CancelInvitationCommand, ApiResponse<bool>>,
    IRequestHandler<ResendInvitationCommand, ApiResponse<bool>>
{
    private readonly IRepository<License> _licenseRepo;
    private readonly IRepository<Subscription> _subscriptionRepo;
    private readonly IRepository<Invitation> _invitationRepo;
    private readonly ITenantContext _tenantContext;
    private readonly INotificationServiceClient _notificationClient;

    public LicenseAndSubscriptionCommandHandler(
        IRepository<License> licenseRepo,
        IRepository<Subscription> subscriptionRepo,
        IRepository<Invitation> invitationRepo,
        ITenantContext tenantContext,
        INotificationServiceClient notificationClient)
    {
        _licenseRepo = licenseRepo;
        _subscriptionRepo = subscriptionRepo;
        _invitationRepo = invitationRepo;
        _tenantContext = tenantContext;
        _notificationClient = notificationClient;
    }

    public async Task<ApiResponse<LicenseDto>> Handle(CreateLicenseCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId)) tenantId = Guid.NewGuid().ToString("N");

        var license = new License(request.OrganizationId, request.Type, request.ExpiresAt, tenantId);
        await _licenseRepo.AddAsync(license, cancellationToken);
        return ApiResponse<LicenseDto>.Ok(Map(license), "License issued successfully.");
    }

    public async Task<ApiResponse<bool>> Handle(AssignLicenseCommand request, CancellationToken cancellationToken)
    {
        var license = await _licenseRepo.GetByIdAsync(request.LicenseId, cancellationToken);
        if (license == null) throw new ResourceNotFoundException(nameof(License), request.LicenseId);

        license.AssignToUser(request.UserId);
        await _licenseRepo.UpdateAsync(license, cancellationToken);
        return ApiResponse<bool>.Ok(true, "License assigned to user.");
    }

    public async Task<ApiResponse<bool>> Handle(RevokeLicenseCommand request, CancellationToken cancellationToken)
    {
        var license = await _licenseRepo.GetByIdAsync(request.LicenseId, cancellationToken);
        if (license == null) throw new ResourceNotFoundException(nameof(License), request.LicenseId);

        license.Revoke();
        await _licenseRepo.UpdateAsync(license, cancellationToken);
        return ApiResponse<bool>.Ok(true, "License revoked.");
    }

    public async Task<ApiResponse<bool>> Handle(RenewLicenseCommand request, CancellationToken cancellationToken)
    {
        var license = await _licenseRepo.GetByIdAsync(request.LicenseId, cancellationToken);
        if (license == null) throw new ResourceNotFoundException(nameof(License), request.LicenseId);

        license.Renew(request.NewExpiration);
        await _licenseRepo.UpdateAsync(license, cancellationToken);
        return ApiResponse<bool>.Ok(true, "License renewed.");
    }

    public async Task<ApiResponse<SubscriptionDto>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId)) tenantId = Guid.NewGuid().ToString("N");

        var sub = new Subscription(request.OrganizationId, request.PlanId, request.BillingCycle, tenantId);
        await _subscriptionRepo.AddAsync(sub, cancellationToken);
        return ApiResponse<SubscriptionDto>.Ok(Map(sub), "Subscription created.");
    }

    public async Task<ApiResponse<bool>> Handle(RenewSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var sub = await _subscriptionRepo.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub == null) throw new ResourceNotFoundException(nameof(Subscription), request.SubscriptionId);

        sub.Renew();
        await _subscriptionRepo.UpdateAsync(sub, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Subscription renewed.");
    }

    public async Task<ApiResponse<bool>> Handle(ChangeSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var sub = await _subscriptionRepo.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (sub == null) throw new ResourceNotFoundException(nameof(Subscription), request.SubscriptionId);

        sub.ChangePlan(request.NewPlanId);
        await _subscriptionRepo.UpdateAsync(sub, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Subscription plan changed.");
    }

    public async Task<ApiResponse<InvitationDto>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(tenantId)) tenantId = Guid.NewGuid().ToString("N");

        var inv = new Invitation(request.TargetEmail, tenantId, request.OrganizationId, request.FamilyId);

        // Send email via notification microservice before persisting to avoid partial state
        await _notificationClient.SendInvitationNotificationAsync(inv.TargetEmail, $"https://lifebalance.app/invite/{inv.Token}", tenantId, cancellationToken);
        await _invitationRepo.AddAsync(inv, cancellationToken);

        return ApiResponse<InvitationDto>.Ok(Map(inv), "Invitation generated and notification dispatched.");
    }

    public async Task<ApiResponse<bool>> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var matches = await _invitationRepo.FindAsync(x => x.Token == request.Token, cancellationToken);
        var inv = matches.FirstOrDefault();
        if (inv == null) throw new ResourceNotFoundException(nameof(Invitation), request.Token);

        inv.Accept();
        await _invitationRepo.UpdateAsync(inv, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Invitation accepted.");
    }

    public async Task<ApiResponse<bool>> Handle(RejectInvitationCommand request, CancellationToken cancellationToken)
    {
        var matches = await _invitationRepo.FindAsync(x => x.Token == request.Token, cancellationToken);
        var inv = matches.FirstOrDefault();
        if (inv == null) throw new ResourceNotFoundException(nameof(Invitation), request.Token);

        inv.Reject();
        await _invitationRepo.UpdateAsync(inv, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Invitation rejected.");
    }

    public async Task<ApiResponse<bool>> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        var inv = await _invitationRepo.GetByIdAsync(request.InvitationId, cancellationToken);
        if (inv == null) throw new ResourceNotFoundException(nameof(Invitation), request.InvitationId);

        inv.Cancel();
        await _invitationRepo.UpdateAsync(inv, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Invitation canceled.");
    }

    public async Task<ApiResponse<bool>> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        var inv = await _invitationRepo.GetByIdAsync(request.InvitationId, cancellationToken);
        if (inv == null) throw new ResourceNotFoundException(nameof(Invitation), request.InvitationId);

        inv.Resend();
        await _invitationRepo.UpdateAsync(inv, cancellationToken);
        await _notificationClient.SendInvitationNotificationAsync(inv.TargetEmail, $"https://lifebalance.app/invite/{inv.Token}", _tenantContext.TenantId, cancellationToken);
        return ApiResponse<bool>.Ok(true, "Invitation resent.");
    }

    private static LicenseDto Map(License l) => new(l.Id, l.OrganizationId, l.TenantId, l.LicenseKey, l.Type, l.Status.ToString(), l.AssignedUserId, l.IssuedAt, l.ExpiresAt);
    private static SubscriptionDto Map(Subscription s) => new(s.Id, s.OrganizationId, s.TenantId, s.PlanId, s.Status.ToString(), s.RenewalDate, s.BillingCycle, s.PaymentHistoryLog);
    private static InvitationDto Map(Invitation i) => new(i.Id, i.TargetEmail, i.TenantId, i.OrganizationId, i.FamilyId, i.Role.ToString(), i.Token, i.Status.ToString(), i.SentAt, i.ExpiresAt);
}

public class LicenseAndSubscriptionQueryHandler :
    IRequestHandler<GetLicenseByIdQuery, ApiResponse<LicenseDto>>,
    IRequestHandler<GetLicensesPagedQuery, ApiResponse<PagedResult<LicenseDto>>>,
    IRequestHandler<GetSubscriptionByIdQuery, ApiResponse<SubscriptionDto>>,
    IRequestHandler<GetSubscriptionsPagedQuery, ApiResponse<PagedResult<SubscriptionDto>>>,
    IRequestHandler<GetInvitationByIdQuery, ApiResponse<InvitationDto>>,
    IRequestHandler<GetInvitationsPagedQuery, ApiResponse<PagedResult<InvitationDto>>>
{
    private readonly IRepository<License> _licenseRepo;
    private readonly IRepository<Subscription> _subRepo;
    private readonly IRepository<Invitation> _invRepo;

    public LicenseAndSubscriptionQueryHandler(
        IRepository<License> licenseRepo,
        IRepository<Subscription> subRepo,
        IRepository<Invitation> invRepo)
    {
        _licenseRepo = licenseRepo;
        _subRepo = subRepo;
        _invRepo = invRepo;
    }

    public async Task<ApiResponse<LicenseDto>> Handle(GetLicenseByIdQuery request, CancellationToken cancellationToken)
    {
        var lic = await _licenseRepo.GetByIdAsync(request.Id, cancellationToken);
        if (lic == null) throw new ResourceNotFoundException(nameof(License), request.Id);
        return ApiResponse<LicenseDto>.Ok(new LicenseDto(lic.Id, lic.OrganizationId, lic.TenantId, lic.LicenseKey, lic.Type, lic.Status.ToString(), lic.AssignedUserId, lic.IssuedAt, lic.ExpiresAt));
    }

    public async Task<ApiResponse<PagedResult<LicenseDto>>> Handle(GetLicensesPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _licenseRepo.GetPagedAsync(x => x.OrganizationId == request.OrganizationId, request.PageIndex, request.PageSize, cancellationToken: cancellationToken);
        var dtos = items.Select(l => new LicenseDto(l.Id, l.OrganizationId, l.TenantId, l.LicenseKey, l.Type, l.Status.ToString(), l.AssignedUserId, l.IssuedAt, l.ExpiresAt));
        return ApiResponse<PagedResult<LicenseDto>>.Ok(new PagedResult<LicenseDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<SubscriptionDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var sub = await _subRepo.GetByIdAsync(request.Id, cancellationToken);
        if (sub == null) throw new ResourceNotFoundException(nameof(Subscription), request.Id);
        return ApiResponse<SubscriptionDto>.Ok(new SubscriptionDto(sub.Id, sub.OrganizationId, sub.TenantId, sub.PlanId, sub.Status.ToString(), sub.RenewalDate, sub.BillingCycle, sub.PaymentHistoryLog));
    }

    public async Task<ApiResponse<PagedResult<SubscriptionDto>>> Handle(GetSubscriptionsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _subRepo.GetPagedAsync(x => true, request.PageIndex, request.PageSize, cancellationToken: cancellationToken);
        var dtos = items.Select(s => new SubscriptionDto(s.Id, s.OrganizationId, s.TenantId, s.PlanId, s.Status.ToString(), s.RenewalDate, s.BillingCycle, s.PaymentHistoryLog));
        return ApiResponse<PagedResult<SubscriptionDto>>.Ok(new PagedResult<SubscriptionDto>(dtos, request.PageIndex, request.PageSize, total));
    }

    public async Task<ApiResponse<InvitationDto>> Handle(GetInvitationByIdQuery request, CancellationToken cancellationToken)
    {
        var inv = await _invRepo.GetByIdAsync(request.Id, cancellationToken);
        if (inv == null) throw new ResourceNotFoundException(nameof(Invitation), request.Id);
        return ApiResponse<InvitationDto>.Ok(new InvitationDto(inv.Id, inv.TargetEmail, inv.TenantId, inv.OrganizationId, inv.FamilyId, inv.Role.ToString(), inv.Token, inv.Status.ToString(), inv.SentAt, inv.ExpiresAt));
    }

    public async Task<ApiResponse<PagedResult<InvitationDto>>> Handle(GetInvitationsPagedQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _invRepo.GetPagedAsync(x => true, request.PageIndex, request.PageSize, cancellationToken: cancellationToken);
        var dtos = items.Select(i => new InvitationDto(i.Id, i.TargetEmail, i.TenantId, i.OrganizationId, i.FamilyId, i.Role.ToString(), i.Token, i.Status.ToString(), i.SentAt, i.ExpiresAt));
        return ApiResponse<PagedResult<InvitationDto>>.Ok(new PagedResult<InvitationDto>(dtos, request.PageIndex, request.PageSize, total));
    }
}
