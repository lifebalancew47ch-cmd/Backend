using FluentAssertions;
using LifeBalance.Reporting.Application.Common;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Application.Exceptions;
using LifeBalance.Reporting.Application.Features.IndividualReport;
using LifeBalance.Reporting.Domain.DomainServices;
using LifeBalance.Reporting.Domain.Enums;
using LifeBalance.Reporting.Domain.ValueObjects;
using NSubstitute;

namespace LifeBalance.Reporting.UnitTests.Features;

public class IndividualReportQueryHandlerTests
{
    private readonly IReportDatasetService _datasetService = Substitute.For<IReportDatasetService>();
    private readonly ISedentaryEngineServiceClient _sedentaryClient = Substitute.For<ISedentaryEngineServiceClient>();
    private readonly IStatisticalAnalyzer _analyzer = new StatisticalAnalyzer();
    private readonly IDateTimeProvider _dateTime = Substitute.For<IDateTimeProvider>();
    private readonly GetIndividualReportQueryHandler _handler;

    public IndividualReportQueryHandlerTests()
    {
        _dateTime.UtcNow.Returns(new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc));
        _handler = new GetIndividualReportQueryHandler(_datasetService, _sedentaryClient, _analyzer, _dateTime);
    }

    private static MedicalReadingDto CreateReading(string userId, DateTime recordedAt, int steps, double heartRate) =>
        new(
            Id: Guid.NewGuid().ToString(),
            UserId: userId,
            FamilyId: null,
            CompanyId: null,
            HeartRate: heartRate,
            Hrv: 60,
            Spo2: 97,
            Steps: steps,
            Latitude: null,
            Longitude: null,
            AccelerometerX: null,
            AccelerometerY: null,
            AccelerometerZ: null,
            GyroscopeX: null,
            GyroscopeY: null,
            GyroscopeZ: null,
            SystolicBp: 120,
            DiastolicBp: 80,
            Weight: 70,
            Height: 175,
            DeviceId: null,
            RecordedAtUtc: recordedAt,
            CreatedAtUtc: recordedAt);

    private static ReportDataset CreateDataset(string userId, params MedicalReadingDto[] readings) =>
        new(
            Scope: ReportScope.Individual,
            ScopeId: userId,
            From: DateTime.UtcNow.AddDays(-1),
            To: DateTime.UtcNow,
            Readings: readings,
            UserProfile: new AuthUserProfileDto(userId, "user@lifebalance.io", "John", "Doe", ["USER"], null, null),
            Members: [],
            Company: null,
            Departments: [],
            Family: null);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsReport()
    {
        var userId = "user-1";
        var reading = CreateReading(userId, new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), 1000, 72);

        _datasetService.BuildAsync(
                Arg.Is(ReportScope.Individual), Arg.Any<string?>(), Arg.Is<string?>(userId), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset(userId, reading));

        _sedentaryClient.GetUserScoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SedentaryScoreDto(userId, 1000, 45, 6, 200, 79.75));

        _sedentaryClient.GetUserGoalsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(
            new GetIndividualReportQuery(userId, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.FullName.Should().Be("John Doe");
        result.Value.Activity.TotalSteps.Should().Be(1000);
        result.Value.Activity.AverageDailySteps.Should().Be(1000);
        result.Value.Activity.AverageActiveMinutes.Should().Be(45);
        result.Value.Sedentary.AverageSedentaryHours.Should().Be(6);
        result.Value.Sedentary.AverageSedentaryScore.Should().Be(79.75);
    }

    [Fact]
    public async Task Handle_MissingSedentaryScore_ThrowsUpstreamUnavailable()
    {
        var userId = "user-1";
        var reading = CreateReading(userId, new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), 1000, 72);

        _datasetService.BuildAsync(
                Arg.Is(ReportScope.Individual), Arg.Any<string?>(), Arg.Is<string?>(userId), Arg.Any<IReadOnlyList<string>>(), Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(CreateDataset(userId, reading));

        _sedentaryClient.GetUserScoreAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SedentaryScoreDto?)null);

        await FluentActions.Awaiting(() => _handler.Handle(
                new GetIndividualReportQuery(userId, null, null),
                CancellationToken.None))
            .Should().ThrowAsync<UpstreamServiceUnavailableException>();
    }
}
