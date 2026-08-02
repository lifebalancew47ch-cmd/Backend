using System.Security.Claims;
using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Exceptions;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class PreferencesControllerTests
{
    private readonly Mock<IPreferenceService> _preferenceService;
    private readonly PreferencesController _controller;

    public PreferencesControllerTests()
    {
        _preferenceService = new Mock<IPreferenceService>();
        _controller = new PreferencesController(_preferenceService.Object);
        SetUser(_controller, "user-1");
    }

    private static void SetUser(ControllerBase controller, string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) });
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static void SetNoUser(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    private static NotificationPreferenceDto BuildPreference() => new()
    {
        Id = "pref-1",
        UserId = "user-1",
        ReceivePush = true,
        ReceiveEmail = true,
        ReceiveWearOS = false,
        Language = "es",
        Timezone = "Europe/Madrid"
    };

    [Fact]
    public async Task Get_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var result = await _controller.Get();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Get();

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Get_CallsServiceWithClaimUserId()
    {
        _preferenceService.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-42");
        await _controller.Get();

        _preferenceService.Verify(s => s.GetAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsServiceResultInData()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Get();

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Data.Should().BeSameAs(preference);
        wrapper.Data!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Get_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Get();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Get_WithoutUserIdClaim_ThrowsApiException()
    {
        SetNoUser(_controller);

        var act = () => _controller.Get();

        await act.Should().ThrowAsync<ApiException>().Where(e => e.StatusCode == 401);
    }

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new UpdatePreferenceDto { ReceivePush = true, QuietModeEnabled = true };
        _preferenceService.Setup(s => s.UpdateAsync("u1", dto)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var result = await _controller.Update(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ResponseWrapsServiceResult()
    {
        var dto = new UpdatePreferenceDto { ReceivePush = true };
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateAsync("u1", dto)).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Update(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Update_CallsServiceWithClaimUserIdAndDto()
    {
        var dto = new UpdatePreferenceDto { ReceiveEmail = false };
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-7");
        await _controller.Update(dto);

        _preferenceService.Verify(s => s.UpdateAsync("user-7", dto), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsServiceResultInData()
    {
        var dto = new UpdatePreferenceDto();
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Update(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Update_ReturnsOkStatusCode()
    {
        var dto = new UpdatePreferenceDto();
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.Update(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdatePush_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var result = await _controller.UpdatePush(true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdatePush_CallsServiceWithClaimUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdatePush(true);

        _preferenceService.Verify(s => s.UpdatePushAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdatePush_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdatePush(false);

        _preferenceService.Verify(s => s.UpdatePushAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdatePush_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdatePush(true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdatePush_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdatePush(true);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateEmail_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var result = await _controller.UpdateEmail(true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmail_CallsServiceWithClaimUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdateEmail(true);

        _preferenceService.Verify(s => s.UpdateEmailAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdateEmail(false);

        _preferenceService.Verify(s => s.UpdateEmailAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdateEmail(true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdateEmail_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdateEmail(true);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateWear_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var result = await _controller.UpdateWear(true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateWear_CallsServiceWithClaimUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdateWear(true);

        _preferenceService.Verify(s => s.UpdateWearOSAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdateWear_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        SetUser(_controller, "user-1");
        await _controller.UpdateWear(false);

        _preferenceService.Verify(s => s.UpdateWearOSAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdateWear_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(preference);

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdateWear(true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdateWear_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(BuildPreference());

        SetUser(_controller, "u1");
        var ok = (OkObjectResult)await _controller.UpdateWear(true);

        ok.StatusCode.Should().Be(200);
    }
}
