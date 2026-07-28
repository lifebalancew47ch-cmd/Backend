using FluentValidation;
using LifeBalance.Dashboard.Application.Features.IndividualDashboard;
using LifeBalance.Dashboard.Application.Features.FamilyDashboard;
using LifeBalance.Dashboard.Application.Features.CompanyDashboard;

namespace LifeBalance.Dashboard.Application.Validators;

public class GetIndividualDashboardQueryValidator : AbstractValidator<GetIndividualDashboardQuery>
{
    public GetIndividualDashboardQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}

public class GetFamilyDashboardQueryValidator : AbstractValidator<GetFamilyDashboardQuery>
{
    public GetFamilyDashboardQueryValidator()
    {
        RuleFor(x => x.FamilyId).NotEmpty().WithMessage("FamilyId is required.");
    }
}

public class GetCompanyDashboardQueryValidator : AbstractValidator<GetCompanyDashboardQuery>
{
    public GetCompanyDashboardQueryValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("CompanyId is required.");
    }
}
