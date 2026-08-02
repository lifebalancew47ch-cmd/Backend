using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class PushControllerTests
{
    private readonly Mock<IPushService> _pushService;
    private readonly PushController _controller;

    public PushControllerTests()
    {
        _pushService = new Mock<IPushService>();
        _controller = new PushController(_pushService.Object);
    }

    private static NotificationResponseDto BuildResponse(string id = "push-1") => new()
    {
        Id = id,
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Channel = NotificationChannel.Push,
        Status = NotificationStatus.Sent
    };

    private static BroadcastPushDto BuildBroadcastDto() => new()
    {
        Title = "Title",
        Body = "Body",
        UserIds = new List<string> { "u1", "u2" },
        Platform = DevicePlatform.Android
    };

    [Fact]
    public async Task Send_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B" };
        _pushService.Setup(s => s.SendAsync(dto)).ReturnsAsync(BuildResponse());

        var result = await _controller.Send(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Send_ResponseWrapsServiceResult()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B" };
        var response = BuildResponse();
        _pushService.Setup(s => s.SendAsync(dto)).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Send(dto);

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Send_CallsServiceWithDto()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B", DeviceTokens = new List<string> { "tok1" } };
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(BuildResponse());

        await _controller.Send(dto);

        _pushService.Verify(s => s.SendAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Send_ReturnsServiceResultInData()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B" };
        var response = BuildResponse("push-42");
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Send(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Data.Should().BeSameAs(response);
        wrapper.Data!.Id.Should().Be("push-42");
    }

    [Fact]
    public async Task Send_ReturnsOkStatusCode()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B" };
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(BuildResponse());

        var ok = (OkObjectResult)await _controller.Send(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Broadcast_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(dto)).ReturnsAsync(new List<NotificationResponseDto> { BuildResponse() });

        var result = await _controller.Broadcast(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Broadcast_ResponseContainsServiceResults()
    {
        var dto = BuildBroadcastDto();
        var results = new List<NotificationResponseDto> { BuildResponse("p1"), BuildResponse("p2") };
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.Broadcast(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task Broadcast_CallsServiceWithDto()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.Broadcast(dto);

        _pushService.Verify(s => s.BroadcastAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Broadcast_ReturnsServiceResultsInData()
    {
        var dto = BuildBroadcastDto();
        var results = new List<NotificationResponseDto> { BuildResponse("p7") };
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.Broadcast(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Data.Should().BeSameAs(results);
        wrapper.Data!.Should().ContainSingle(x => x.Id == "p7");
    }

    [Fact]
    public async Task Broadcast_ReturnsOkStatusCode()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.Broadcast(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendWear_OverridesExistingPlatformToWearOS()
    {
        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B", Platform = DevicePlatform.Android };
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(BuildResponse());

        await _controller.SendWear(dto);

        dto.Platform.Should().Be(DevicePlatform.WearOS);
    }

    [Fact]
    public async Task SendWear_CallsServiceWithDtoForcedToWearOS()
    {
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(BuildResponse());

        await _controller.SendWear(new SendPushDto { UserId = "u1", Title = "T", Body = "B" });

        _pushService.Verify(s => s.SendAsync(It.Is<SendPushDto>(d => d.Platform == DevicePlatform.WearOS)), Times.Once);
    }

    [Fact]
    public async Task SendWear_ReturnsOkObjectResult()
    {
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(BuildResponse());

        var result = await _controller.SendWear(new SendPushDto { UserId = "u1", Title = "T", Body = "B" });

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendWear_ReturnsServiceResultWrapped()
    {
        var response = BuildResponse("wear-1");
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.SendWear(new SendPushDto { UserId = "u1", Title = "T", Body = "B" });

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task SendWear_WhenPlatformNull_SetsWearOSBeforeCallingService()
    {
        SendPushDto? captured = null;
        _pushService.Setup(s => s.SendAsync(It.IsAny<SendPushDto>()))
            .Callback<SendPushDto>(d => captured = d)
            .ReturnsAsync(BuildResponse());

        var dto = new SendPushDto { UserId = "u1", Title = "T", Body = "B" };
        await _controller.SendWear(dto);

        captured.Should().NotBeNull();
        captured!.Platform.Should().Be(DevicePlatform.WearOS);
    }

    [Fact]
    public async Task SendToCompany_ReturnsOkObjectResult()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var result = await _controller.SendToCompany(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendToCompany_CallsBroadcastAsync()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToCompany(dto);

        _pushService.Verify(s => s.BroadcastAsync(dto), Times.Once);
    }

    [Fact]
    public async Task SendToCompany_ResponseContainsServiceResults()
    {
        var dto = BuildBroadcastDto();
        var results = new List<NotificationResponseDto> { BuildResponse("c1") };
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendToCompany(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendToCompany_ReturnsOkStatusCode()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendToCompany(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendToCompany_PassesDtoToServiceUnchanged()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToCompany(dto);

        _pushService.Verify(s => s.BroadcastAsync(It.Is<BroadcastPushDto>(d =>
            d.Title == "Title" && d.Body == "Body" && d.OrganizationId == null)), Times.Once);
    }

    [Fact]
    public async Task SendToFamily_ReturnsOkObjectResult()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var result = await _controller.SendToFamily(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendToFamily_CallsBroadcastAsync()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToFamily(dto);

        _pushService.Verify(s => s.BroadcastAsync(dto), Times.Once);
    }

    [Fact]
    public async Task SendToFamily_ResponseContainsServiceResults()
    {
        var dto = BuildBroadcastDto();
        var results = new List<NotificationResponseDto> { BuildResponse("f1") };
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendToFamily(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendToFamily_ReturnsOkStatusCode()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendToFamily(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendToFamily_PassesDtoToServiceUnchanged()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToFamily(dto);

        _pushService.Verify(s => s.BroadcastAsync(It.Is<BroadcastPushDto>(d =>
            d.Title == "Title" && d.Body == "Body" && d.FamilyId == null)), Times.Once);
    }

    [Fact]
    public async Task SendToDepartment_ReturnsOkObjectResult()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var result = await _controller.SendToDepartment(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendToDepartment_CallsBroadcastAsync()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToDepartment(dto);

        _pushService.Verify(s => s.BroadcastAsync(dto), Times.Once);
    }

    [Fact]
    public async Task SendToDepartment_ResponseContainsServiceResults()
    {
        var dto = BuildBroadcastDto();
        var results = new List<NotificationResponseDto> { BuildResponse("d1") };
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendToDepartment(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendToDepartment_ReturnsOkStatusCode()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendToDepartment(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendToDepartment_PassesDtoToServiceUnchanged()
    {
        var dto = BuildBroadcastDto();
        _pushService.Setup(s => s.BroadcastAsync(It.IsAny<BroadcastPushDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendToDepartment(dto);

        _pushService.Verify(s => s.BroadcastAsync(It.Is<BroadcastPushDto>(d =>
            d.Title == "Title" && d.Body == "Body" && d.DepartmentId == null)), Times.Once);
    }
}
