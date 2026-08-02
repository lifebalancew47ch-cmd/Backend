using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<IHistoryService> _historyService;
    private readonly Mock<IPreferenceService> _preferenceService;
    private readonly Mock<IScheduleService> _scheduleService;
    private readonly Mock<ITemplateService> _templateService;
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _notificationService = new Mock<INotificationService>();
        _historyService = new Mock<IHistoryService>();
        _preferenceService = new Mock<IPreferenceService>();
        _scheduleService = new Mock<IScheduleService>();
        _templateService = new Mock<ITemplateService>();
        _controller = new NotificationsController(
            _notificationService.Object,
            _historyService.Object,
            _preferenceService.Object,
            _scheduleService.Object,
            _templateService.Object);
    }

    private static NotificationResponseDto BuildResponse(string id = "notif-1") => new()
    {
        Id = id,
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Type = NotificationType.Information,
        Channel = NotificationChannel.Push,
        Status = NotificationStatus.Sent,
        CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0)
    };

    [Fact]
    public async Task Send_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new SendNotificationDto { UserId = "user-1", Title = "Title", Body = "Body" };
        _notificationService.Setup(s => s.SendAsync(dto)).ReturnsAsync(BuildResponse());

        var result = await _controller.Send(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Send_ResponseWrapsServiceResult()
    {
        var dto = new SendNotificationDto { UserId = "user-1", Title = "Title", Body = "Body" };
        var response = BuildResponse();
        _notificationService.Setup(s => s.SendAsync(dto)).ReturnsAsync(response);

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
        var dto = new SendNotificationDto { UserId = "user-1", Title = "Title", Body = "Body" };
        _notificationService.Setup(s => s.SendAsync(It.IsAny<SendNotificationDto>())).ReturnsAsync(BuildResponse());

        await _controller.Send(dto);

        _notificationService.Verify(s => s.SendAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Send_ReturnsServiceResultInData()
    {
        var dto = new SendNotificationDto { UserId = "user-1", Title = "Title", Body = "Body" };
        var response = BuildResponse("notif-42");
        _notificationService.Setup(s => s.SendAsync(It.IsAny<SendNotificationDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Send(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Data.Should().BeSameAs(response);
        wrapper.Data!.Id.Should().Be("notif-42");
    }

    [Fact]
    public async Task Send_ReturnsOkStatusCode()
    {
        var dto = new SendNotificationDto { UserId = "user-1", Title = "Title", Body = "Body" };
        _notificationService.Setup(s => s.SendAsync(It.IsAny<SendNotificationDto>())).ReturnsAsync(BuildResponse());

        var ok = (OkObjectResult)await _controller.Send(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendBulk_WithMultipleDtos_ReturnsOkObjectResult()
    {
        var dtos = new List<SendNotificationDto>
        {
            new() { UserId = "u1", Title = "T1", Body = "B1" },
            new() { UserId = "u2", Title = "T2", Body = "B2" }
        };
        var results = new List<NotificationResponseDto> { BuildResponse("n1"), BuildResponse("n2") };
        _notificationService.Setup(s => s.SendBulkAsync(dtos)).ReturnsAsync(results);

        var result = await _controller.SendBulk(dtos);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendBulk_ResponseContainsAllServiceResults()
    {
        var dtos = new List<SendNotificationDto> { new() { UserId = "u1", Title = "T1", Body = "B1" } };
        var results = new List<NotificationResponseDto> { BuildResponse("n1") };
        _notificationService.Setup(s => s.SendBulkAsync(It.IsAny<List<SendNotificationDto>>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendBulk(dtos);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendBulk_CallsServiceWithList()
    {
        var dtos = new List<SendNotificationDto> { new() { UserId = "u1", Title = "T1", Body = "B1" } };
        _notificationService.Setup(s => s.SendBulkAsync(It.IsAny<List<SendNotificationDto>>()))
            .ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendBulk(dtos);

        _notificationService.Verify(s => s.SendBulkAsync(dtos), Times.Once);
    }

    [Fact]
    public async Task SendBulk_WithEmptyList_ReturnsOkWithEmptyData()
    {
        _notificationService.Setup(s => s.SendBulkAsync(It.IsAny<List<SendNotificationDto>>()))
            .ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendBulk(new List<SendNotificationDto>());

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SendBulk_ReturnsOkStatusCode()
    {
        var dtos = new List<SendNotificationDto> { new() { UserId = "u1", Title = "T1", Body = "B1" } };
        _notificationService.Setup(s => s.SendBulkAsync(It.IsAny<List<SendNotificationDto>>()))
            .ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendBulk(dtos);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Schedule_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new ScheduleNotificationDto { UserId = "u1", Title = "T", Body = "B", ScheduledFor = DateTime.UtcNow.AddHours(1) };
        _notificationService.Setup(s => s.ScheduleAsync(dto)).ReturnsAsync(BuildResponse());

        var result = await _controller.Schedule(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Schedule_ResponseWrapsServiceResult()
    {
        var dto = new ScheduleNotificationDto { UserId = "u1", Title = "T", Body = "B", ScheduledFor = DateTime.UtcNow.AddHours(1) };
        var response = BuildResponse("sched-1");
        _notificationService.Setup(s => s.ScheduleAsync(dto)).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Schedule(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Schedule_CallsServiceWithDto()
    {
        var dto = new ScheduleNotificationDto { UserId = "u1", Title = "T", Body = "B", ScheduledFor = DateTime.UtcNow.AddHours(1) };
        _notificationService.Setup(s => s.ScheduleAsync(It.IsAny<ScheduleNotificationDto>())).ReturnsAsync(BuildResponse());

        await _controller.Schedule(dto);

        _notificationService.Verify(s => s.ScheduleAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Schedule_ReturnsServiceResultInData()
    {
        var dto = new ScheduleNotificationDto { UserId = "u1", Title = "T", Body = "B", ScheduledFor = DateTime.UtcNow.AddHours(1) };
        var response = BuildResponse("sched-9");
        _notificationService.Setup(s => s.ScheduleAsync(It.IsAny<ScheduleNotificationDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Schedule(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Schedule_ReturnsOkStatusCode()
    {
        var dto = new ScheduleNotificationDto { UserId = "u1", Title = "T", Body = "B", ScheduledFor = DateTime.UtcNow.AddHours(1) };
        _notificationService.Setup(s => s.ScheduleAsync(It.IsAny<ScheduleNotificationDto>())).ReturnsAsync(BuildResponse());

        var ok = (OkObjectResult)await _controller.Schedule(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_WithAllFilters_ReturnsOkObjectResult()
    {
        var results = new List<NotificationResponseDto> { BuildResponse() };
        _notificationService.Setup(s => s.GetAllAsync("u1", "o1", "f1", "d1")).ReturnsAsync(results);

        var result = await _controller.GetAll("u1", "o1", "f1", "d1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ResponseContainsServiceResults()
    {
        var results = new List<NotificationResponseDto> { BuildResponse("n1") };
        _notificationService.Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.GetAll("u1", "o1", "f1", "d1");

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task GetAll_CallsServiceWithAllFilters()
    {
        _notificationService.Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.GetAll("u1", "o1", "f1", "d1");

        _notificationService.Verify(s => s.GetAllAsync("u1", "o1", "f1", "d1"), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithoutFilters_CallsServiceWithNullArguments()
    {
        _notificationService.Setup(s => s.GetAllAsync(null, null, null, null)).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.GetAll(null, null, null, null);

        _notificationService.Verify(s => s.GetAllAsync(null, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsOkStatusCode()
    {
        _notificationService.Setup(s => s.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.GetAll(null, null, null, null);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenNotificationExists_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.GetByIdAsync("notif-1")).ReturnsAsync(BuildResponse());

        var result = await _controller.GetById("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotificationExists_ResponseWrapsServiceResult()
    {
        var response = BuildResponse("notif-5");
        _notificationService.Setup(s => s.GetByIdAsync("notif-5")).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.GetById("notif-5");

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((NotificationResponseDto?)null);

        var result = await _controller.GetById("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ResponseMessageIsNotificationNotFound()
    {
        _notificationService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((NotificationResponseDto?)null);

        var nf = (NotFoundObjectResult)await _controller.GetById("missing");

        nf.StatusCode.Should().Be(404);
        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found");
        wrapper.Data.Should().BeNull();
    }

    [Fact]
    public async Task GetById_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(BuildResponse());

        await _controller.GetById("notif-9");

        _notificationService.Verify(s => s.GetByIdAsync("notif-9"), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.DeleteAsync("notif-1")).ReturnsAsync(true);

        var result = await _controller.Delete("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.DeleteAsync("notif-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Delete("notif-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notification deleted");
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.DeleteAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Delete("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsFalse_ResponseMessageIsNotificationNotFound()
    {
        _notificationService.Setup(s => s.DeleteAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Delete("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found");
    }

    [Fact]
    public async Task Delete_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Delete("notif-7");

        _notificationService.Verify(s => s.DeleteAsync("notif-7"), Times.Once);
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.CancelAsync("notif-1")).ReturnsAsync(true);

        var result = await _controller.Cancel("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.CancelAsync("notif-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Cancel("notif-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notification cancelled");
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.CancelAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Cancel("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_WhenServiceReturnsFalse_ResponseMessageIsNotFoundOrAlreadySent()
    {
        _notificationService.Setup(s => s.CancelAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Cancel("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found or already sent");
    }

    [Fact]
    public async Task Cancel_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.CancelAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Cancel("notif-3");

        _notificationService.Verify(s => s.CancelAsync("notif-3"), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.MarkAsReadAsync("notif-1")).ReturnsAsync(true);

        var result = await _controller.MarkAsRead("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.MarkAsReadAsync("notif-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.MarkAsRead("notif-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notification marked as read");
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.MarkAsReadAsync("missing")).ReturnsAsync(false);

        var result = await _controller.MarkAsRead("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MarkAsRead_WhenServiceReturnsFalse_ResponseMessageIsNotificationNotFound()
    {
        _notificationService.Setup(s => s.MarkAsReadAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.MarkAsRead("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found");
    }

    [Fact]
    public async Task MarkAsRead_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.MarkAsReadAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.MarkAsRead("notif-8");

        _notificationService.Verify(s => s.MarkAsReadAsync("notif-8"), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.MarkAllAsReadAsync("u1")).ReturnsAsync(true);

        var result = await _controller.MarkAllAsRead("u1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsSuccessResponseWithMessage()
    {
        _notificationService.Setup(s => s.MarkAllAsReadAsync("u1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.MarkAllAsRead("u1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notifications marked as read");
    }

    [Fact]
    public async Task MarkAllAsRead_CallsServiceWithUserId()
    {
        _notificationService.Setup(s => s.MarkAllAsReadAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.MarkAllAsRead("user-42");

        _notificationService.Verify(s => s.MarkAllAsReadAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_WhenServiceReturnsFalse_StillReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.MarkAllAsReadAsync("u1")).ReturnsAsync(false);

        var ok = (OkObjectResult)await _controller.MarkAllAsRead("u1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().Be("Notifications marked as read");
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOkStatusCode()
    {
        _notificationService.Setup(s => s.MarkAllAsReadAsync("u1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.MarkAllAsRead("u1");

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Archive_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.ArchiveAsync("notif-1")).ReturnsAsync(true);

        var result = await _controller.Archive("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Archive_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.ArchiveAsync("notif-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Archive("notif-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notification archived");
    }

    [Fact]
    public async Task Archive_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.ArchiveAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Archive("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Archive_WhenServiceReturnsFalse_ResponseMessageIsNotificationNotFound()
    {
        _notificationService.Setup(s => s.ArchiveAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Archive("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found");
    }

    [Fact]
    public async Task Archive_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.ArchiveAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Archive("notif-6");

        _notificationService.Verify(s => s.ArchiveAsync("notif-6"), Times.Once);
    }

    [Fact]
    public async Task Favorite_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _notificationService.Setup(s => s.FavoriteAsync("notif-1")).ReturnsAsync(true);

        var result = await _controller.Favorite("notif-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Favorite_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _notificationService.Setup(s => s.FavoriteAsync("notif-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Favorite("notif-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Notification favorite toggled");
    }

    [Fact]
    public async Task Favorite_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _notificationService.Setup(s => s.FavoriteAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Favorite("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Favorite_WhenServiceReturnsFalse_ResponseMessageIsNotificationNotFound()
    {
        _notificationService.Setup(s => s.FavoriteAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Favorite("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Notification not found");
    }

    [Fact]
    public async Task Favorite_CallsServiceWithGivenId()
    {
        _notificationService.Setup(s => s.FavoriteAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Favorite("notif-4");

        _notificationService.Verify(s => s.FavoriteAsync("notif-4"), Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsOkWithoutResponseWrapper()
    {
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(new List<NotificationHistoryDto>());

        var result = await _controller.GetUserNotifications("u1", 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<List<NotificationItemDto>>();
    }

    [Fact]
    public async Task GetUserNotifications_MapsHistoryDtoToItemDto()
    {
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0);
        var history = new List<NotificationHistoryDto>
        {
            new()
            {
                Id = "n1",
                Title = "Title",
                Body = "Body",
                Type = NotificationType.Warning,
                CreatedAt = createdAt,
                IsRead = true
            }
        };
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(history);

        var ok = (OkObjectResult)await _controller.GetUserNotifications("u1", 10);

        var items = ok.Value.Should().BeOfType<List<NotificationItemDto>>().Subject;
        items.Should().ContainSingle();
        items[0].Should().Be(new NotificationItemDto("n1", "Title", "Body", "Warning", createdAt, true));
    }

    [Fact]
    public async Task GetUserNotifications_CallsHistoryServiceWithUserId()
    {
        _historyService.Setup(s => s.GetByUserAsync(It.IsAny<string>())).ReturnsAsync(new List<NotificationHistoryDto>());

        await _controller.GetUserNotifications("user-42", 10);

        _historyService.Verify(s => s.GetByUserAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task GetUserNotifications_AppliesLimitToResults()
    {
        var history = Enumerable.Range(1, 12)
            .Select(i => new NotificationHistoryDto { Id = $"n{i}", Title = "T", Body = "B", Type = NotificationType.Information })
            .ToList();
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(history);

        var ok = (OkObjectResult)await _controller.GetUserNotifications("u1", 10);

        var items = ok.Value.Should().BeOfType<List<NotificationItemDto>>().Subject;
        items.Should().HaveCount(10);
        items[0].Id.Should().Be("n1");
    }

    [Fact]
    public async Task GetUserNotifications_WhenNoHistory_ReturnsEmptyList()
    {
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(new List<NotificationHistoryDto>());

        var ok = (OkObjectResult)await _controller.GetUserNotifications("u1", 10);

        var items = ok.Value.Should().BeOfType<List<NotificationItemDto>>().Subject;
        items.Should().BeEmpty();
    }
}
