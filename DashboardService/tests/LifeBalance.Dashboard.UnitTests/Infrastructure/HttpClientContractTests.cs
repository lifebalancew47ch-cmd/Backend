using System.Net;
using System.Text;
using FluentAssertions;
using LifeBalance.Dashboard.Application.Common.Interfaces;
using LifeBalance.Dashboard.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeBalance.Dashboard.UnitTests.Infrastructure;

/// <summary>
/// Contract tests that feed the exact JSON payloads returned by the deployed
/// upstream services into the real Dashboard HttpClients, proving the DTOs
/// deserialize correctly (envelope unwrapping, route and field-name mapping).
/// Payloads captured live on 2026-08-06 against the Render deployments.
/// </summary>
public class HttpClientContractTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses;

        private readonly List<string> _requestedPaths = new();

        public IReadOnlyList<string> RequestedPaths => _requestedPaths;

        public StubHttpMessageHandler(Dictionary<string, string> responses) => _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (_responses.TryGetValue(path, out var json))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static HttpClient CreateClient(Dictionary<string, string> responses, out StubHttpMessageHandler handler)
    {
        handler = new StubHttpMessageHandler(responses);
        return new HttpClient(handler) { BaseAddress = new Uri("https://upstream.test") };
    }

    private const string UserId = "6a73d8f56e0ce06544cee215";

    [Fact]
    public async Task MedicalDataServiceClient_UnwrapsEnvelope_ReturnsBiometrics()
    {
        var json = "{\"success\":true,\"message\":\"Operation completed successfully.\"," +
                   $"\"data\":{{\"userId\":\"{UserId}\",\"heartRate\":78,\"systolicBp\":0,\"diastolicBp\":0,\"weight\":0,\"height\":0,\"bmi\":0,\"recordedAt\":\"2026-08-06T01:20:00Z\"}},\"errors\":[]}}";

        var http = CreateClient(new Dictionary<string, string> { [$"/api/v1/medical/biometrics/{UserId}"] = json }, out _);
        var sut = new MedicalDataServiceClient(http, NullLogger<MedicalDataServiceClient>.Instance);

        var result = await sut.GetUserBiometricsAsync(UserId);

        result.Should().NotBeNull();
        result!.HeartRate.Should().Be(78);
        result.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task SedentaryEngineServiceClient_ProgressAndScore_MapToActivity()
    {
        var progressJson = "{\"success\":true,\"message\":\"Operation completed successfully.\"," +
                           "\"data\":{\"dailySteps\":5230,\"dailyStepsTarget\":8000,\"activeMinutes\":45,\"activeMinutesTarget\":30,\"stepsProgress\":65.375,\"activeProgress\":100},\"errors\":[]}";
        var scoreJson = "{\"success\":true,\"message\":\"Operation completed successfully.\"," +
                        $"\"data\":{{\"userId\":\"{UserId}\",\"score\":79.75,\"riskLevel\":\"Low\",\"recordedAtUtc\":\"2026-08-06T01:00:00Z\"}},\"errors\":[]}}";

        var http = CreateClient(new Dictionary<string, string>
        {
            ["/api/v1/sedentary/progress"] = progressJson,
            ["/api/v1/sedentary/score"] = scoreJson
        }, out var handler);
        var sut = new SedentaryEngineServiceClient(http, NullLogger<SedentaryEngineServiceClient>.Instance);

        var result = await sut.GetUserActivityAsync(UserId);

        handler.RequestedPaths.Should().Contain(p => p.Contains("/api/v1/sedentary/progress"));
        handler.RequestedPaths.Should().Contain(p => p.Contains("/api/v1/sedentary/score"));
        result.Should().NotBeNull();
        result!.DailySteps.Should().Be(5230);
        result.ActiveMinutes.Should().Be(45);
        result.HourlyHeatmap.Should().HaveCount(24);
    }

    [Fact]
    public async Task SedentaryEngineServiceClient_ScoreEndpointMissing_StillReturnsProgress()
    {
        var progressJson = "{\"success\":true,\"message\":\"ok\"," +
                           "\"data\":{\"dailySteps\":3000,\"dailyStepsTarget\":8000,\"activeMinutes\":20,\"activeMinutesTarget\":30,\"stepsProgress\":37.5,\"activeProgress\":66.6},\"errors\":[]}";

        var http = CreateClient(new Dictionary<string, string> { ["/api/v1/sedentary/progress"] = progressJson }, out _);
        var sut = new SedentaryEngineServiceClient(http, NullLogger<SedentaryEngineServiceClient>.Instance);

        var result = await sut.GetUserActivityAsync(UserId);

        result.Should().NotBeNull();
        result!.DailySteps.Should().Be(3000);
        result.ActiveMinutes.Should().Be(20);
    }

    [Fact]
    public async Task GamificationServiceClient_UnwrapsEnvelope_ReturnsRewards()
    {
        var json = "{\"success\":true,\"message\":\"Operation completed successfully.\"," +
                   $"\"data\":{{\"userId\":\"{UserId}\",\"points\":1200,\"badgesUnlocked\":4,\"currentStreakDays\":7,\"recentRewards\":[\"step-champion\"]}},\"errors\":[]}}";

        var http = CreateClient(new Dictionary<string, string> { [$"/api/v1/gamification/user/{UserId}/rewards"] = json }, out _);
        var sut = new GamificationServiceClient(http, NullLogger<GamificationServiceClient>.Instance);

        var result = await sut.GetUserRewardsAsync(UserId);

        result.Should().NotBeNull();
        result!.Points.Should().Be(1200);
        result.CurrentStreakDays.Should().Be(7);
        result.RecentRewards.Should().Contain("step-champion");
    }

    [Fact]
    public async Task MlPredictionServiceClient_UnwrapsEnvelope_ReturnsRecommendations()
    {
        var json = "{\"success\":true,\"message\":\"Operation completed successfully.\"," +
                   $"\"data\":[{{\"recommendationId\":\"r1\",\"category\":\"SedentaryHealth\",\"title\":\"Muévete\",\"description\":\"Registra más actividad\",\"priorityScore\":0.2}}],\"errors\":[]}}";

        var http = CreateClient(new Dictionary<string, string> { [$"/api/v1/ml/recommendations/{UserId}"] = json }, out _);
        var sut = new MlPredictionServiceClient(http, NullLogger<MlPredictionServiceClient>.Instance);

        var result = await sut.GetRecommendationsAsync(UserId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].RecommendationId.Should().Be("r1");
        result[0].PriorityScore.Should().Be(0.2);
    }

    [Fact]
    public async Task NotificationServiceClient_UsesUserRouteAndMapsFieldNames()
    {
        var json = "[{\"id\":\"n1\",\"title\":\"Título\",\"body\":\"Mensaje\",\"type\":\"Info\",\"createdAt\":\"2026-08-06T00:00:00Z\",\"isRead\":false}]";

        var http = CreateClient(new Dictionary<string, string> { ["/api/v1/notifications/user"] = json }, out var handler);
        var sut = new NotificationServiceClient(http, NullLogger<NotificationServiceClient>.Instance);

        var result = await sut.GetUserNotificationsAsync(UserId);

        handler.RequestedPaths.Should().Contain("/api/v1/notifications/user?limit=10");
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result[0].Id.Should().Be("n1");
        result[0].Message.Should().Be("Mensaje");
        result[0].Severity.Should().Be("Info");
        result[0].Read.Should().BeFalse();
    }

    [Fact]
    public async Task NotificationServiceClient_ClampsLimit()
    {
        var json = "[]";
        var http = CreateClient(new Dictionary<string, string> { ["/api/v1/notifications/user"] = json }, out var handler);
        var sut = new NotificationServiceClient(http, NullLogger<NotificationServiceClient>.Instance);

        await sut.GetUserNotificationsAsync(UserId, limit: 500);

        handler.RequestedPaths.Should().Contain("/api/v1/notifications/user?limit=100");
    }
}
