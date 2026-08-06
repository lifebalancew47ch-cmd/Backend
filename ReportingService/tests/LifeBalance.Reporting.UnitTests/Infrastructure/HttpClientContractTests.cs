using System.Net;
using System.Text;
using FluentAssertions;
using LifeBalance.Reporting.Application.Common.Interfaces;
using LifeBalance.Reporting.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging.Abstractions;

namespace LifeBalance.Reporting.UnitTests.Infrastructure;

public class HttpClientContractTests
{
    private const string UserId = "6a73d8f56e0ce06544cee215";

    [Fact]
    public async Task GetUserScoreAsync_MergesProgressAndScore()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/sedentary/progress")
            {
                return JsonResponse(200, """
                {"success":true,"data":{"dailySteps":5230,"dailyStepsTarget":8000,"activeMinutes":45,"activeMinutesTarget":30,"stepsProgress":65.375,"activeProgress":100}}
                """);
            }

            if (request.RequestUri.AbsolutePath == "/api/v1/sedentary/score")
            {
                return JsonResponse(200, """
                {"success":true,"data":{"userId":"6a73d8f56e0ce06544cee215","score":79.75,"riskLevel":"Low","recordedAtUtc":"2026-08-06T01:00:00Z"}}
                """);
            }

            return JsonResponse(404, "{}");
        });

        var client = CreateSedentaryClient(handler);

        var result = await client.GetUserScoreAsync(UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(UserId);
        result.DailySteps.Should().Be(5230);
        result.ActiveMinutes.Should().Be(45);
        result.Score.Should().Be(79.75);
        handler.RequestedPaths.Should().Contain("/api/v1/sedentary/progress");
        handler.RequestedPaths.Should().Contain("/api/v1/sedentary/score");
    }

    [Fact]
    public async Task GetUserScoreAsync_AllEndpointsFail_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(404, "{}"));

        var client = CreateSedentaryClient(handler);

        var result = await client.GetUserScoreAsync(UserId, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserHistoryAsync_MapsDailyRecords()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/sedentary/users/6a73d8f56e0ce06544cee215/history");
            return JsonResponse(200, """
            {"success":true,"data":[{"date":"2026-08-05T00:00:00Z","sedentaryScore":71.5,"sedentaryHours":8,"activeMinutes":35,"steps":4800,"breakCount":4}]}
            """);
        });

        var client = CreateSedentaryClient(handler);

        var result = await client.GetUserHistoryAsync(UserId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].SedentaryScore.Should().Be(71.5);
        result[0].SedentaryHours.Should().Be(8);
        result[0].ActiveMinutes.Should().Be(35);
        result[0].Steps.Should().Be(4800);
        result[0].BreakCount.Should().Be(4);
    }

    [Fact]
    public async Task GetUserReadingsAsync_UsesHistoryAndEnrichesLatestWithBiometrics()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/medical/history")
            {
                return JsonResponse(200, """
                {"success":true,"data":[{"id":"reading-1","userId":"6a73d8f56e0ce06544cee215","heartRate":78,"hrv":45,"spo2":97,"steps":5230,"latitude":null,"longitude":null,"recordedAtUtc":"2026-08-06T01:20:00Z"}]}
                """);
            }

            if (request.RequestUri.AbsolutePath == "/api/v1/medical/biometrics/6a73d8f56e0ce06544cee215")
            {
                return JsonResponse(200, """
                {"success":true,"data":{"userId":"6a73d8f56e0ce06544cee215","heartRate":78,"systolicBp":120,"diastolicBp":80,"weight":70,"height":175,"bmi":22.9,"recordedAt":"2026-08-06T01:20:00Z"}}
                """);
            }

            return JsonResponse(404, "{}");
        });

        var client = CreateMedicalClient(handler);

        var result = await client.GetUserReadingsAsync(UserId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        handler.RequestedPaths.Should().Contain("/api/v1/medical/history");
        handler.RequestedPaths.Should().Contain("/api/v1/medical/biometrics/6a73d8f56e0ce06544cee215");
        result![0].HeartRate.Should().Be(78);
        result[0].Hrv.Should().Be(45);
        result[0].Spo2.Should().Be(97);
        result[0].Steps.Should().Be(5230);
        result[0].SystolicBp.Should().Be(120);
        result[0].DiastolicBp.Should().Be(80);
        result[0].Weight.Should().Be(70);
        result[0].Height.Should().Be(175);
    }

    [Fact]
    public async Task GetUserReadingsAsync_HistoryUnavailable_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(404, "{}"));

        var client = CreateMedicalClient(handler);

        var result = await client.GetUserReadingsAsync(UserId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestBiometricsAsync_UsesBiometricsRoute()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/v1/medical/biometrics/6a73d8f56e0ce06544cee215");
            return JsonResponse(200, """
            {"success":true,"data":{"userId":"6a73d8f56e0ce06544cee215","heartRate":78,"systolicBp":120,"diastolicBp":80,"weight":70,"height":175,"bmi":22.9,"recordedAt":"2026-08-06T01:20:00Z"}}
            """);
        });

        var client = CreateMedicalClient(handler);

        var result = await client.GetLatestBiometricsAsync(UserId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(UserId);
        result.HeartRate.Should().Be(78);
        result.SystolicBp.Should().Be(120);
        result.RecordedAtUtc.Should().Be(new DateTime(2026, 8, 6, 1, 20, 0, DateTimeKind.Utc));
    }

    private static SedentaryEngineServiceClient CreateSedentaryClient(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://sedentary.test") },
            NullLogger<SedentaryEngineServiceClient>.Instance);

    private static MedicalDataServiceClient CreateMedicalClient(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://medical.test") },
            NullLogger<MedicalDataServiceClient>.Instance);

    private static HttpResponseMessage JsonResponse(int statusCode, string json) =>
        new((HttpStatusCode)statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public IList<string> RequestedPaths { get; } = new List<string>();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedPaths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(_responder(request));
        }
    }
}
