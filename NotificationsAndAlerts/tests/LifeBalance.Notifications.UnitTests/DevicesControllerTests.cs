using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class DevicesControllerTests
{
    private readonly Mock<IDeviceRegistrationService> _deviceService;
    private readonly DevicesController _controller;

    public DevicesControllerTests()
    {
        _deviceService = new Mock<IDeviceRegistrationService>();
        _controller = new DevicesController(_deviceService.Object);
    }

    private static DeviceRegistrationDto BuildRegistrationDto() => new()
    {
        UserId = "user-1",
        DeviceToken = "token-1",
        Platform = DevicePlatform.Android
    };

    [Fact]
    public async Task Register_ReturnsOkObjectResult()
    {
        _deviceService.Setup(s => s.RegisterAsync(It.IsAny<DeviceRegistrationDto>())).Returns(Task.CompletedTask);

        var result = await _controller.Register(BuildRegistrationDto());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Register_ReturnsSuccessResponse()
    {
        _deviceService.Setup(s => s.RegisterAsync(It.IsAny<DeviceRegistrationDto>())).Returns(Task.CompletedTask);

        var ok = (OkObjectResult)await _controller.Register(BuildRegistrationDto());

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Device registered");
    }

    [Fact]
    public async Task Register_CallsServiceWithDto()
    {
        var dto = BuildRegistrationDto();
        _deviceService.Setup(s => s.RegisterAsync(It.IsAny<DeviceRegistrationDto>())).Returns(Task.CompletedTask);

        await _controller.Register(dto);

        _deviceService.Verify(s => s.RegisterAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsOkStatusCode()
    {
        _deviceService.Setup(s => s.RegisterAsync(It.IsAny<DeviceRegistrationDto>())).Returns(Task.CompletedTask);

        var ok = (OkObjectResult)await _controller.Register(BuildRegistrationDto());

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Register_ResponseHasSuccessTrue()
    {
        _deviceService.Setup(s => s.RegisterAsync(It.IsAny<DeviceRegistrationDto>())).Returns(Task.CompletedTask);

        var ok = (OkObjectResult)await _controller.Register(BuildRegistrationDto());

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Unregister_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _deviceService.Setup(s => s.UnregisterAsync("u1", "tok1")).ReturnsAsync(true);

        var result = await _controller.Unregister("u1", "tok1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Unregister_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _deviceService.Setup(s => s.UnregisterAsync("u1", "tok1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Unregister("u1", "tok1");

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Device unregistered");
    }

    [Fact]
    public async Task Unregister_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _deviceService.Setup(s => s.UnregisterAsync("u1", "tok1")).ReturnsAsync(false);

        var result = await _controller.Unregister("u1", "tok1");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Unregister_WhenServiceReturnsFalse_ResponseMessageIsDeviceNotFound()
    {
        _deviceService.Setup(s => s.UnregisterAsync("u1", "tok1")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Unregister("u1", "tok1");

        nf.StatusCode.Should().Be(404);
        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Device not found");
    }

    [Fact]
    public async Task Unregister_CallsServiceWithUserIdAndToken()
    {
        _deviceService.Setup(s => s.UnregisterAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Unregister("user-42", "token-99");

        _deviceService.Verify(s => s.UnregisterAsync("user-42", "token-99"), Times.Once);
    }
}
