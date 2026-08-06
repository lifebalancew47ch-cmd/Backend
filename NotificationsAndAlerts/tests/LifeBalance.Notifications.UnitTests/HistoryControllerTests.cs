using System.Security.Claims;
using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Exceptions;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class HistoryControllerTests
{
    private readonly Mock<IHistoryService> _historyService;
    private readonly HistoryController _controller;

    public HistoryControllerTests()
    {
        _historyService = new Mock<IHistoryService>();
        _controller = new HistoryController(_historyService.Object);
        SetUser(_controller, "user-1");
    }

    private static void SetUser(ControllerBase controller, string userId, string? organizationId = null, string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (organizationId is not null) claims.Add(new Claim("organization_id", organizationId));
        if (role is not null) claims.Add(new Claim("role", role));
        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.NameIdentifier, "role");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static void SetNoUser(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static NotificationHistoryDto BuildHistoryItem(string id = "h-1") => new()
    {
        Id = id,
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Type = NotificationType.Information,
        Channel = NotificationChannel.Push,
        Status = NotificationStatus.Sent,
        CreatedAt = new DateTime(2026, 1, 1, 8, 0, 0)
    };

    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        var result = new PaginatedResult<NotificationHistoryDto> { Items = { BuildHistoryItem() }, TotalCount = 1, Page = 1, PageSize = 20 };
        _historyService.Setup(s => s.GetAllAsync(1, 20)).ReturnsAsync(result);

        var response = await _controller.GetAll();

        response.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ResponseContainsPaginatedResult()
    {
        var result = new PaginatedResult<NotificationHistoryDto>
        {
            Items = { BuildHistoryItem("h1"), BuildHistoryItem("h2") },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };
        _historyService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(result);

        var ok = (OkObjectResult)await _controller.GetAll(1, 20);

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<PaginatedResult<NotificationHistoryDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(result);
    }

    [Fact]
    public async Task GetAll_CallsServiceWithPageAndPageSize()
    {
        _historyService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedResult<NotificationHistoryDto>());

        await _controller.GetAll(3, 50);

        _historyService.Verify(s => s.GetAllAsync(3, 50), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithDefaults_CallsServiceWithPageOneAndSizeTwenty()
    {
        _historyService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedResult<NotificationHistoryDto>());

        await _controller.GetAll();

        _historyService.Verify(s => s.GetAllAsync(1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAll_ReturnsOkStatusCode()
    {
        _historyService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PaginatedResult<NotificationHistoryDto>());

        var ok = (OkObjectResult)await _controller.GetAll(1, 20);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetByUser_ReturnsOkObjectResult()
    {
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(new List<NotificationHistoryDto> { BuildHistoryItem() });

        SetUser(_controller, "u1");
        var result = await _controller.GetByUser();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByUser_ResponseContainsServiceResults()
    {
        var items = new List<NotificationHistoryDto> { BuildHistoryItem("h5") };
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(items);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.GetByUser();

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationHistoryDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(items);
    }

    [Fact]
    public async Task GetByUser_CallsServiceWithClaimUserId()
    {
        _historyService.Setup(s => s.GetByUserAsync(It.IsAny<string>())).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "user-42");
        await _controller.GetByUser();

        _historyService.Verify(s => s.GetByUserAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task GetByUser_WhenNoHistory_ReturnsEmptyData()
    {
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.GetByUser();

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationHistoryDto>>>().Subject;
        wrapper.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUser_ReturnsOkStatusCode()
    {
        _historyService.Setup(s => s.GetByUserAsync("u1")).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.GetByUser();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetByUser_WithoutUserIdClaim_ThrowsApiException()
    {
        SetNoUser(_controller);

        var act = () => _controller.GetByUser();

        await act.Should().ThrowAsync<ApiException>().Where(e => e.StatusCode == 401);
    }

    // A partir de la auditoria del 6/08/2026 (S-20), GetByOrganization exige
    // que el organizationId de la ruta coincida con el claim del caller
    // (o que el caller sea ADMIN). Los tests de "camino feliz" ahora fijan
    // ese claim explicitamente; los nuevos tests de abajo cubren el fallo
    // de autorizacion que estaba presente antes del fix.

    [Fact]
    public async Task GetByOrganization_ReturnsOkObjectResult()
    {
        _historyService.Setup(s => s.GetByOrganizationAsync("o1"))
            .ReturnsAsync(new List<NotificationHistoryDto> { BuildHistoryItem() });

        SetUser(_controller, "user-1", organizationId: "o1");
        var result = await _controller.GetByOrganization("o1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByOrganization_ResponseContainsServiceResults()
    {
        var items = new List<NotificationHistoryDto> { BuildHistoryItem("h9") };
        _historyService.Setup(s => s.GetByOrganizationAsync("o1")).ReturnsAsync(items);

        SetUser(_controller, "user-1", organizationId: "o1");
        var ok = (OkObjectResult)await _controller.GetByOrganization("o1");

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationHistoryDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(items);
    }

    [Fact]
    public async Task GetByOrganization_CallsServiceWithOrganizationId()
    {
        _historyService.Setup(s => s.GetByOrganizationAsync(It.IsAny<string>())).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "user-1", organizationId: "org-42");
        await _controller.GetByOrganization("org-42");

        _historyService.Verify(s => s.GetByOrganizationAsync("org-42"), Times.Once);
    }

    [Fact]
    public async Task GetByOrganization_WhenNoHistory_ReturnsEmptyData()
    {
        _historyService.Setup(s => s.GetByOrganizationAsync("o1")).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "user-1", organizationId: "o1");
        var ok = (OkObjectResult)await _controller.GetByOrganization("o1");

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationHistoryDto>>>().Subject;
        wrapper.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByOrganization_ReturnsOkStatusCode()
    {
        _historyService.Setup(s => s.GetByOrganizationAsync("o1")).ReturnsAsync(new List<NotificationHistoryDto>());

        SetUser(_controller, "user-1", organizationId: "o1");
        var ok = (OkObjectResult)await _controller.GetByOrganization("o1");

        ok.StatusCode.Should().Be(200);
    }

    // --- Regresion S-20: BOLA en history/organization/{organizationId} ---

    [Fact]
    public async Task GetByOrganization_WithMismatchedOrganization_ReturnsForbid()
    {
        // Usuario autenticado de la organizacion "o1" pide el historial de "o2".
        _historyService.Setup(s => s.GetByOrganizationAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<NotificationHistoryDto> { BuildHistoryItem() });

        SetUser(_controller, "user-1", organizationId: "o1");
        var result = await _controller.GetByOrganization("o2");

        result.Should().BeOfType<ForbidResult>();
        _historyService.Verify(s => s.GetByOrganizationAsync(It.IsAny<string>()), Times.Never,
            "no debe consultarse el historial de una organizacion ajena");
    }

    [Fact]
    public async Task GetByOrganization_WithoutOrganizationClaim_ReturnsForbid()
    {
        // Token sin claim organization_id (p. ej. cuenta personal sin tenant).
        SetUser(_controller, "user-1");
        var result = await _controller.GetByOrganization("o1");

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetByOrganization_AsAdmin_AllowsAnyOrganization()
    {
        _historyService.Setup(s => s.GetByOrganizationAsync("o-ajena"))
            .ReturnsAsync(new List<NotificationHistoryDto> { BuildHistoryItem() });

        SetUser(_controller, "admin-1", organizationId: "o-propia", role: "ADMIN");
        var result = await _controller.GetByOrganization("o-ajena");

        result.Should().BeOfType<OkObjectResult>();
    }
}
