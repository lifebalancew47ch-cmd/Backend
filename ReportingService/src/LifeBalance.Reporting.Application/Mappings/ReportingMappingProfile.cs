using AutoMapper;
using LifeBalance.Reporting.Application.Features.ReportHistory;
using LifeBalance.Reporting.Domain.Entities;

namespace LifeBalance.Reporting.Application.Mappings;

/// <summary>
/// AutoMapper profile for the Reporting service.
/// </summary>
public sealed class ReportingMappingProfile : Profile
{
    /// <summary>Initializes a new instance of <see cref="ReportingMappingProfile"/>.</summary>
    public ReportingMappingProfile()
    {
        CreateMap<ReportGenerationLog, ReportHistoryItemDto>();
    }
}
