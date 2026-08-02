using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class MetricsControllerTests
{
    private readonly Mock<IMetricsService> _metricsService;
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _metricsService = new Mock<IMetricsService>();
        _controller = new MetricsController(_metricsService.Object);
    }

    [Fact]
    public async Task GetGeneral_ReturnsOkObjectResult()
    {
        _metricsService.Setup(s => s.GetGeneralAsync()).ReturnsAsync(new MetricsDto());

        var result = await _controller.GetGeneral();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetGeneral_ResponseWrapsServiceResult()
    {
        var metrics = new MetricsDto { TotalSent = 100, Delivered = 90, Failed = 10 };
        _metricsService.Setup(s => s.GetGeneralAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetGeneral();

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<MetricsDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(metrics);
    }

    [Fact]
    public async Task GetGeneral_CallsService()
    {
        _metricsService.Setup(s => s.GetGeneralAsync()).ReturnsAsync(new MetricsDto());

        await _controller.GetGeneral();

        _metricsService.Verify(s => s.GetGeneralAsync(), Times.Once);
    }

    [Fact]
    public async Task GetGeneral_ReturnsServiceResultInData()
    {
        var metrics = new MetricsDto { TotalSent = 42, MostUsedChannel = "Push" };
        _metricsService.Setup(s => s.GetGeneralAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetGeneral();

        var wrapper = ok.Value.Should().BeOfType<Response<MetricsDto>>().Subject;
        wrapper.Data.Should().BeSameAs(metrics);
        wrapper.Data!.TotalSent.Should().Be(42);
    }

    [Fact]
    public async Task GetGeneral_ReturnsOkStatusCode()
    {
        _metricsService.Setup(s => s.GetGeneralAsync()).ReturnsAsync(new MetricsDto());

        var ok = (OkObjectResult)await _controller.GetGeneral();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetDelivery_ReturnsOkObjectResult()
    {
        _metricsService.Setup(s => s.GetDeliveryAsync()).ReturnsAsync(new DeliveryMetricsDto());

        var result = await _controller.GetDelivery();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDelivery_ResponseWrapsServiceResult()
    {
        var metrics = new DeliveryMetricsDto { TotalAttempts = 200, SuccessfulDeliveries = 180, SuccessRate = 0.9 };
        _metricsService.Setup(s => s.GetDeliveryAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetDelivery();

        var wrapper = ok.Value.Should().BeOfType<Response<DeliveryMetricsDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(metrics);
    }

    [Fact]
    public async Task GetDelivery_CallsService()
    {
        _metricsService.Setup(s => s.GetDeliveryAsync()).ReturnsAsync(new DeliveryMetricsDto());

        await _controller.GetDelivery();

        _metricsService.Verify(s => s.GetDeliveryAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDelivery_ReturnsServiceResultInData()
    {
        var metrics = new DeliveryMetricsDto { TotalAttempts = 99 };
        _metricsService.Setup(s => s.GetDeliveryAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetDelivery();

        var wrapper = ok.Value.Should().BeOfType<Response<DeliveryMetricsDto>>().Subject;
        wrapper.Data.Should().BeSameAs(metrics);
        wrapper.Data!.TotalAttempts.Should().Be(99);
    }

    [Fact]
    public async Task GetDelivery_ReturnsOkStatusCode()
    {
        _metricsService.Setup(s => s.GetDeliveryAsync()).ReturnsAsync(new DeliveryMetricsDto());

        var ok = (OkObjectResult)await _controller.GetDelivery();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetChannels_ReturnsOkObjectResult()
    {
        _metricsService.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelMetricsDto>());

        var result = await _controller.GetChannels();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetChannels_ResponseContainsServiceResults()
    {
        var channels = new List<ChannelMetricsDto>
        {
            new() { Channel = "Push", Count = 60, Percentage = 60.0 },
            new() { Channel = "Email", Count = 40, Percentage = 40.0 }
        };
        _metricsService.Setup(s => s.GetChannelsAsync()).ReturnsAsync(channels);

        var ok = (OkObjectResult)await _controller.GetChannels();

        var wrapper = ok.Value.Should().BeOfType<Response<List<ChannelMetricsDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(channels);
    }

    [Fact]
    public async Task GetChannels_CallsService()
    {
        _metricsService.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelMetricsDto>());

        await _controller.GetChannels();

        _metricsService.Verify(s => s.GetChannelsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetChannels_ReturnsServiceResultsInData()
    {
        var channels = new List<ChannelMetricsDto> { new() { Channel = "Push", Count = 7 } };
        _metricsService.Setup(s => s.GetChannelsAsync()).ReturnsAsync(channels);

        var ok = (OkObjectResult)await _controller.GetChannels();

        var wrapper = ok.Value.Should().BeOfType<Response<List<ChannelMetricsDto>>>().Subject;
        wrapper.Data.Should().BeSameAs(channels);
        wrapper.Data!.Should().ContainSingle(x => x.Channel == "Push");
    }

    [Fact]
    public async Task GetChannels_ReturnsOkStatusCode()
    {
        _metricsService.Setup(s => s.GetChannelsAsync()).ReturnsAsync(new List<ChannelMetricsDto>());

        var ok = (OkObjectResult)await _controller.GetChannels();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetErrors_ReturnsOkObjectResult()
    {
        _metricsService.Setup(s => s.GetErrorsAsync()).ReturnsAsync(new ErrorMetricsDto());

        var result = await _controller.GetErrors();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetErrors_ResponseWrapsServiceResult()
    {
        var metrics = new ErrorMetricsDto { TotalErrors = 5 };
        _metricsService.Setup(s => s.GetErrorsAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetErrors();

        var wrapper = ok.Value.Should().BeOfType<Response<ErrorMetricsDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(metrics);
    }

    [Fact]
    public async Task GetErrors_CallsService()
    {
        _metricsService.Setup(s => s.GetErrorsAsync()).ReturnsAsync(new ErrorMetricsDto());

        await _controller.GetErrors();

        _metricsService.Verify(s => s.GetErrorsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetErrors_ReturnsServiceResultInData()
    {
        var metrics = new ErrorMetricsDto { TotalErrors = 3 };
        _metricsService.Setup(s => s.GetErrorsAsync()).ReturnsAsync(metrics);

        var ok = (OkObjectResult)await _controller.GetErrors();

        var wrapper = ok.Value.Should().BeOfType<Response<ErrorMetricsDto>>().Subject;
        wrapper.Data.Should().BeSameAs(metrics);
        wrapper.Data!.TotalErrors.Should().Be(3);
    }

    [Fact]
    public async Task GetErrors_ReturnsOkStatusCode()
    {
        _metricsService.Setup(s => s.GetErrorsAsync()).ReturnsAsync(new ErrorMetricsDto());

        var ok = (OkObjectResult)await _controller.GetErrors();

        ok.StatusCode.Should().Be(200);
    }
}
