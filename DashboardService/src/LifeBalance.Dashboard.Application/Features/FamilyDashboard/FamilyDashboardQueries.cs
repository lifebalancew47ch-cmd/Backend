using MediatR;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Shared.Results;

namespace LifeBalance.Dashboard.Application.Features.FamilyDashboard;

public record GetFamilyDashboardQuery(string FamilyId) : IRequest<Result<FamilyDashboardResponse>>;
public record GetFamilyStatisticsQuery(string FamilyId) : IRequest<Result<FamilyStatisticsResponse>>;
public record GetFamilyGoalsQuery(string FamilyId) : IRequest<Result<FamilyGoalsResponse>>;
public record GetFamilyRankingQuery(string FamilyId) : IRequest<Result<FamilyRankingResponse>>;
public record GetFamilyMembersQuery(string FamilyId) : IRequest<Result<FamilyMembersResponse>>;
public record GetFamilyChallengesQuery(string FamilyId) : IRequest<Result<FamilyChallengesResponse>>;
public record GetFamilyRewardsQuery(string FamilyId) : IRequest<Result<FamilyRewardsResponse>>;
public record GetFamilyHeatmapQuery(string FamilyId) : IRequest<Result<FamilyHeatmapResponse>>;

public class FamilyDashboardQueryHandlers :
    IRequestHandler<GetFamilyDashboardQuery, Result<FamilyDashboardResponse>>,
    IRequestHandler<GetFamilyStatisticsQuery, Result<FamilyStatisticsResponse>>,
    IRequestHandler<GetFamilyGoalsQuery, Result<FamilyGoalsResponse>>,
    IRequestHandler<GetFamilyRankingQuery, Result<FamilyRankingResponse>>,
    IRequestHandler<GetFamilyMembersQuery, Result<FamilyMembersResponse>>,
    IRequestHandler<GetFamilyChallengesQuery, Result<FamilyChallengesResponse>>,
    IRequestHandler<GetFamilyRewardsQuery, Result<FamilyRewardsResponse>>,
    IRequestHandler<GetFamilyHeatmapQuery, Result<FamilyHeatmapResponse>>
{
    private readonly IAuthServiceClient _authClient;
    private readonly IMedicalDataServiceClient _medicalClient;
    private readonly IGamificationServiceClient _gamificationClient;

    public FamilyDashboardQueryHandlers(
        IAuthServiceClient authClient,
        IMedicalDataServiceClient medicalClient,
        IGamificationServiceClient gamificationClient)
    {
        _authClient = authClient;
        _medicalClient = medicalClient;
        _gamificationClient = gamificationClient;
    }

    public async Task<Result<FamilyDashboardResponse>> Handle(GetFamilyDashboardQuery request, CancellationToken cancellationToken)
    {
        var membersTask = _authClient.GetFamilyMembersProfileAsync(request.FamilyId, cancellationToken);
        var biometricsTask = _medicalClient.GetFamilyBiometricsAsync(request.FamilyId, cancellationToken);
        var challengesTask = _gamificationClient.GetFamilyChallengesAsync(request.FamilyId, cancellationToken);

        await Task.WhenAll(membersTask, biometricsTask, challengesTask);

        var members = await membersTask ?? new List<AuthUserResponseDto>();
        var biometrics = await biometricsTask ?? new List<MedicalDataResponseDto>();
        var challenges = await challengesTask ?? new List<ChallengeProgressDto>();

        return Result.Success(new FamilyDashboardResponse(request.FamilyId, members, biometrics, challenges));
    }

    public async Task<Result<FamilyStatisticsResponse>> Handle(GetFamilyStatisticsQuery request, CancellationToken cancellationToken)
    {
        var members = await _authClient.GetFamilyMembersProfileAsync(request.FamilyId, cancellationToken);
        int count = members?.Count ?? 0;
        return Result.Success(new FamilyStatisticsResponse(request.FamilyId, count, count * 7500, 45.0));
    }

    public async Task<Result<FamilyGoalsResponse>> Handle(GetFamilyGoalsQuery request, CancellationToken cancellationToken)
    {
        var challenges = await _gamificationClient.GetFamilyChallengesAsync(request.FamilyId, cancellationToken);
        return Result.Success(new FamilyGoalsResponse(request.FamilyId, challenges ?? new List<ChallengeProgressDto>()));
    }

    public async Task<Result<FamilyRankingResponse>> Handle(GetFamilyRankingQuery request, CancellationToken cancellationToken)
    {
        var members = await _authClient.GetFamilyMembersProfileAsync(request.FamilyId, cancellationToken) ?? new List<AuthUserResponseDto>();
        var rankings = members.Select((m, index) => new FamilyMemberRankDto(m.UserId, $"{m.FirstName} {m.LastName}", 1000 - (index * 100), index + 1)).ToList();
        return Result.Success(new FamilyRankingResponse(request.FamilyId, rankings));
    }

    public async Task<Result<FamilyMembersResponse>> Handle(GetFamilyMembersQuery request, CancellationToken cancellationToken)
    {
        var members = await _authClient.GetFamilyMembersProfileAsync(request.FamilyId, cancellationToken);
        return Result.Success(new FamilyMembersResponse(request.FamilyId, members ?? new List<AuthUserResponseDto>()));
    }

    public async Task<Result<FamilyChallengesResponse>> Handle(GetFamilyChallengesQuery request, CancellationToken cancellationToken)
    {
        var challenges = await _gamificationClient.GetFamilyChallengesAsync(request.FamilyId, cancellationToken);
        return Result.Success(new FamilyChallengesResponse(request.FamilyId, challenges ?? new List<ChallengeProgressDto>()));
    }

    public async Task<Result<FamilyRewardsResponse>> Handle(GetFamilyRewardsQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new FamilyRewardsResponse(request.FamilyId, 4500, new List<string> { "Family Champion", "Together Strong" }));
    }

    public async Task<Result<FamilyHeatmapResponse>> Handle(GetFamilyHeatmapQuery request, CancellationToken cancellationToken)
    {
        return Result.Success(new FamilyHeatmapResponse(request.FamilyId, Enumerable.Repeat(5, 24).ToList()));
    }
}
