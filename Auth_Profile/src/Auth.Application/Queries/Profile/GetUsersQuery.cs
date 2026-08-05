using Auth.Application.DTOs.Profile;
using Auth.Shared.Common;
using MediatR;

namespace Auth.Application.Queries.Profile;

public class GetUsersQuery : IRequest<ApiResponse<PagedResult<UserProfileDto>>>
{
    public int Page { get; }
    public int PageSize { get; }

    public GetUsersQuery(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }
}
