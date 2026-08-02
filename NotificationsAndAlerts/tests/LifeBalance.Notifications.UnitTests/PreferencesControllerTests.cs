using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
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

        var result = await _controller.Get("u1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.Get("u1");

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Get_CallsServiceWithUserId()
    {
        _preferenceService.Setup(s => s.GetAsync(It.IsAny<string>())).ReturnsAsync(BuildPreference());

        await _controller.Get("user-42");

        _preferenceService.Verify(s => s.GetAsync("user-42"), Times.Once);
    }

    [Fact]
    public async Task Get_ReturnsServiceResultInData()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.Get("u1");

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Data.Should().BeSameAs(preference);
        wrapper.Data!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Get_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.GetAsync("u1")).ReturnsAsync(BuildPreference());

        var ok = (OkObjectResult)await _controller.Get("u1");

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new UpdatePreferenceDto { ReceivePush = true, QuietModeEnabled = true };
        _preferenceService.Setup(s => s.UpdateAsync("u1", dto)).ReturnsAsync(BuildPreference());

        var result = await _controller.Update("u1", dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_ResponseWrapsServiceResult()
    {
        var dto = new UpdatePreferenceDto { ReceivePush = true };
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateAsync("u1", dto)).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.Update("u1", dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Update_CallsServiceWithUserIdAndDto()
    {
        var dto = new UpdatePreferenceDto { ReceiveEmail = false };
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(BuildPreference());

        await _controller.Update("user-7", dto);

        _preferenceService.Verify(s => s.UpdateAsync("user-7", dto), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsServiceResultInData()
    {
        var dto = new UpdatePreferenceDto();
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.Update("u1", dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task Update_ReturnsOkStatusCode()
    {
        var dto = new UpdatePreferenceDto();
        _preferenceService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<UpdatePreferenceDto>())).ReturnsAsync(BuildPreference());

        var ok = (OkObjectResult)await _controller.Update("u1", dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdatePush_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(BuildPreference());

        var result = await _controller.UpdatePush("u1", true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdatePush_CallsServiceWithUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdatePush("user-1", true);

        _preferenceService.Verify(s => s.UpdatePushAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdatePush_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdatePush("user-1", false);

        _preferenceService.Verify(s => s.UpdatePushAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdatePush_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.UpdatePush("u1", true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdatePush_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdatePushAsync("u1", true)).ReturnsAsync(BuildPreference());

        var ok = (OkObjectResult)await _controller.UpdatePush("u1", true);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateEmail_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(BuildPreference());

        var result = await _controller.UpdateEmail("u1", true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateEmail_CallsServiceWithUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdateEmail("user-1", true);

        _preferenceService.Verify(s => s.UpdateEmailAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdateEmail("user-1", false);

        _preferenceService.Verify(s => s.UpdateEmailAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdateEmail_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.UpdateEmail("u1", true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdateEmail_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdateEmailAsync("u1", true)).ReturnsAsync(BuildPreference());

        var ok = (OkObjectResult)await _controller.UpdateEmail("u1", true);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateWear_ReturnsOkObjectResult()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(BuildPreference());

        var result = await _controller.UpdateWear("u1", true);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateWear_CallsServiceWithUserIdAndEnabled()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdateWear("user-1", true);

        _preferenceService.Verify(s => s.UpdateWearOSAsync("user-1", true), Times.Once);
    }

    [Fact]
    public async Task UpdateWear_CallsServiceWithFalseWhenDisabled()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(BuildPreference());

        await _controller.UpdateWear("user-1", false);

        _preferenceService.Verify(s => s.UpdateWearOSAsync("user-1", false), Times.Once);
    }

    [Fact]
    public async Task UpdateWear_ResponseWrapsServiceResult()
    {
        var preference = BuildPreference();
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(preference);

        var ok = (OkObjectResult)await _controller.UpdateWear("u1", true);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationPreferenceDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(preference);
    }

    [Fact]
    public async Task UpdateWear_ReturnsOkStatusCode()
    {
        _preferenceService.Setup(s => s.UpdateWearOSAsync("u1", true)).ReturnsAsync(BuildPreference());

        var ok = (OkObjectResult)await _controller.UpdateWear("u1", true);

        ok.StatusCode.Should().Be(200);
    }
}
