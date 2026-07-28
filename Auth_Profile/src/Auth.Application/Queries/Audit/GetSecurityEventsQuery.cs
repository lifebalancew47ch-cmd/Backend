using Auth.Application.DTOs.Audit;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Audit;

public record GetSecurityEventsQuery(int Page = 1, int PageSize = 20) : IRequest<ApiResponse<PagedResult<AuditLogDto>>>;
