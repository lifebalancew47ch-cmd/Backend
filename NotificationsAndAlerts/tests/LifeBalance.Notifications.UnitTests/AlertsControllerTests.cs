using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _alertService;
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        _alertService = new Mock<IAlertService>();
        _controller = new AlertsController(_alertService.Object);
    }

    private static AlertDto BuildAlert(string id = "alert-1") => new()
    {
        Id = id,
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Source = "source",
        Priority = AlertPriority.Critical,
        CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0)
    };

    private static CreateAlertDto BuildCreateDto() => new()
    {
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Source = "source",
        Priority = AlertPriority.Critical
    };

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = BuildCreateDto();
        _alertService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(BuildAlert());

        var result = await _controller.Create(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ResponseWrapsServiceResult()
    {
        var dto = BuildCreateDto();
        var alert = BuildAlert("alert-5");
        _alertService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(alert);

        var ok = (OkObjectResult)await _controller.Create(dto);

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<AlertDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(alert);
    }

    [Fact]
    public async Task Create_CallsServiceWithDto()
    {
        var dto = BuildCreateDto();
        _alertService.Setup(s => s.CreateAsync(It.IsAny<CreateAlertDto>())).ReturnsAsync(BuildAlert());

        await _controller.Create(dto);

        _alertService.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsServiceResultInData()
    {
        var dto = BuildCreateDto();
        var alert = BuildAlert("alert-42");
        _alertService.Setup(s => s.CreateAsync(It.IsAny<CreateAlertDto>())).ReturnsAsync(alert);

        var ok = (OkObjectResult)await _controller.Create(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<AlertDto>>().Subject;
        wrapper.Data.Should().BeSameAs(alert);
    }

    [Fact]
    public async Task Create_ReturnsOkStatusCode()
    {
        var dto = BuildCreateDto();
        _alertService.Setup(s => s.CreateAsync(It.IsAny<CreateAlertDto>())).ReturnsAsync(BuildAlert());

        var ok = (OkObjectResult)await _controller.Create(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        _alertService.Setup(s => s.GetAllAsync("u1")).ReturnsAsync(new List<AlertDto> { BuildAlert() });

        var result = await _controller.GetAll("u1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ResponseContainsServiceResults()
    {
        var alerts = new List<AlertDto> { BuildAlert("a1"), BuildAlert("a2") };
        _alertService.Setup(s => s.GetAllAsync("u1")).ReturnsAsync(alerts);

        var ok = (OkObjectResult)await _controller.GetAll("u1");

        var wrapper = ok.Value.Should().BeOfType<Response<List<AlertDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(alerts);
    }

    [Fact]
    public async Task GetAll_CallsServiceWithUserId()
    {
        _alertService.Setup(s => s.GetAllAsync(It.IsAny<string>())).ReturnsAsync(new List<AlertDto>());

        await _controller.GetAll("user-42");

        _alertService.Verify(s => s.GetAllAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenNoAlerts_ReturnsEmptyData()
    {
        _alertService.Setup(s => s.GetAllAsync("u1")).ReturnsAsync(new List<AlertDto>());

        var ok = (OkObjectResult)await _controller.GetAll("u1");

        var wrapper = ok.Value.Should().BeOfType<Response<List<AlertDto>>>().Subject;
        wrapper.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsOkStatusCode()
    {
        _alertService.Setup(s => s.GetAllAsync("u1")).ReturnsAsync(new List<AlertDto>());

        var ok = (OkObjectResult)await _controller.GetAll("u1");

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenAlertExists_ReturnsOkObjectResult()
    {
        _alertService.Setup(s => s.GetByIdAsync("alert-1")).ReturnsAsync(BuildAlert());

        var result = await _controller.GetById("alert-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenAlertExists_ResponseWrapsServiceResult()
    {
        var alert = BuildAlert("alert-3");
        _alertService.Setup(s => s.GetByIdAsync("alert-3")).ReturnsAsync(alert);

        var ok = (OkObjectResult)await _controller.GetById("alert-3");

        var wrapper = ok.Value.Should().BeOfType<Response<AlertDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(alert);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFoundObjectResult()
    {
        _alertService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((AlertDto?)null);

        var result = await _controller.GetById("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ResponseMessageIsAlertNotFound()
    {
        _alertService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((AlertDto?)null);

        var nf = (NotFoundObjectResult)await _controller.GetById("missing");

        nf.StatusCode.Should().Be(404);
        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Alert not found");
    }

    [Fact]
    public async Task GetById_CallsServiceWithGivenId()
    {
        _alertService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(BuildAlert());

        await _controller.GetById("alert-9");

        _alertService.Verify(s => s.GetByIdAsync("alert-9"), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _alertService.Setup(s => s.MarkAsReadAsync("alert-1")).ReturnsAsync(true);

        var result = await _controller.MarkAsRead("alert-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _alertService.Setup(s => s.MarkAsReadAsync("alert-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.MarkAsRead("alert-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Alert marked as read");
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _alertService.Setup(s => s.MarkAsReadAsync("missing")).ReturnsAsync(false);

        var result = await _controller.MarkAsRead("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsFalse_ResponseMessageIsAlertNotFound()
    {
        _alertService.Setup(s => s.MarkAsReadAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.MarkAsRead("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Alert not found");
    }

    [Fact]
    public async Task MarkAsRead_CallsServiceWithGivenId()
    {
        _alertService.Setup(s => s.MarkAsReadAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.MarkAsRead("alert-7");

        _alertService.Verify(s => s.MarkAsReadAsync("alert-7"), Times.Once);
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _alertService.Setup(s => s.DismissAsync("alert-1")).ReturnsAsync(true);

        var result = await _controller.Dismiss("alert-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _alertService.Setup(s => s.DismissAsync("alert-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Dismiss("alert-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Alert dismissed");
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _alertService.Setup(s => s.DismissAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Dismiss("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Dismiss_WhenServiceReturnsFalse_ResponseMessageIsAlertNotFound()
    {
        _alertService.Setup(s => s.DismissAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Dismiss("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Alert not found");
    }

    [Fact]
    public async Task Dismiss_CallsServiceWithGivenId()
    {
        _alertService.Setup(s => s.DismissAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Dismiss("alert-4");

        _alertService.Verify(s => s.DismissAsync("alert-4"), Times.Once);
    }
}
