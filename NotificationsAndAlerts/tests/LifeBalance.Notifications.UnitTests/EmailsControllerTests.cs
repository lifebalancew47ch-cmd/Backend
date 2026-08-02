using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class EmailsControllerTests
{
    private readonly Mock<IEmailService> _emailService;
    private readonly EmailsController _controller;

    public EmailsControllerTests()
    {
        _emailService = new Mock<IEmailService>();
        _controller = new EmailsController(_emailService.Object);
    }

    private static NotificationResponseDto BuildResponse(string id = "email-1") => new()
    {
        Id = id,
        UserId = "user-1",
        Title = "Title",
        Body = "Body",
        Type = NotificationType.Information,
        Channel = NotificationChannel.Email,
        Status = NotificationStatus.Sent
    };

    [Fact]
    public async Task Send_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new SendEmailDto { To = "a@test.com", Subject = "S", Body = "B" };
        _emailService.Setup(s => s.SendAsync(dto)).ReturnsAsync(BuildResponse());

        var result = await _controller.Send(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Send_ResponseWrapsServiceResult()
    {
        var dto = new SendEmailDto { To = "a@test.com", Subject = "S", Body = "B" };
        var response = BuildResponse();
        _emailService.Setup(s => s.SendAsync(dto)).ReturnsAsync(response);

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
        var dto = new SendEmailDto { To = "a@test.com", Subject = "S", Body = "B", IsHtml = true };
        _emailService.Setup(s => s.SendAsync(It.IsAny<SendEmailDto>())).ReturnsAsync(BuildResponse());

        await _controller.Send(dto);

        _emailService.Verify(s => s.SendAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Send_ReturnsServiceResultInData()
    {
        var dto = new SendEmailDto { To = "a@test.com", Subject = "S", Body = "B" };
        var response = BuildResponse("email-42");
        _emailService.Setup(s => s.SendAsync(It.IsAny<SendEmailDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.Send(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Send_ReturnsOkStatusCode()
    {
        var dto = new SendEmailDto { To = "a@test.com", Subject = "S", Body = "B" };
        _emailService.Setup(s => s.SendAsync(It.IsAny<SendEmailDto>())).ReturnsAsync(BuildResponse());

        var ok = (OkObjectResult)await _controller.Send(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendTemplate_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new EmailTemplateDto { To = new List<string> { "a@test.com" }, TemplateId = "tpl-1" };
        _emailService.Setup(s => s.SendTemplateAsync(dto)).ReturnsAsync(BuildResponse());

        var result = await _controller.SendTemplate(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendTemplate_ResponseWrapsServiceResult()
    {
        var dto = new EmailTemplateDto { To = new List<string> { "a@test.com" }, TemplateId = "tpl-1" };
        var response = BuildResponse("email-2");
        _emailService.Setup(s => s.SendTemplateAsync(dto)).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.SendTemplate(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task SendTemplate_CallsServiceWithDto()
    {
        var dto = new EmailTemplateDto
        {
            To = new List<string> { "a@test.com" },
            TemplateId = "tpl-1",
            Variables = new Dictionary<string, string> { ["name"] = "Ana" }
        };
        _emailService.Setup(s => s.SendTemplateAsync(It.IsAny<EmailTemplateDto>())).ReturnsAsync(BuildResponse());

        await _controller.SendTemplate(dto);

        _emailService.Verify(s => s.SendTemplateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task SendTemplate_ReturnsServiceResultInData()
    {
        var dto = new EmailTemplateDto { To = new List<string> { "a@test.com" }, TemplateId = "tpl-1" };
        var response = BuildResponse("email-7");
        _emailService.Setup(s => s.SendTemplateAsync(It.IsAny<EmailTemplateDto>())).ReturnsAsync(response);

        var ok = (OkObjectResult)await _controller.SendTemplate(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<NotificationResponseDto>>().Subject;
        wrapper.Data.Should().BeSameAs(response);
    }

    [Fact]
    public async Task SendTemplate_ReturnsOkStatusCode()
    {
        var dto = new EmailTemplateDto { To = new List<string> { "a@test.com" }, TemplateId = "tpl-1" };
        _emailService.Setup(s => s.SendTemplateAsync(It.IsAny<EmailTemplateDto>())).ReturnsAsync(BuildResponse());

        var ok = (OkObjectResult)await _controller.SendTemplate(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SendBulk_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = new BulkEmailDto { To = new List<string> { "a@test.com" }, Subject = "S", Body = "B" };
        _emailService.Setup(s => s.SendBulkAsync(dto)).ReturnsAsync(new List<NotificationResponseDto> { BuildResponse() });

        var result = await _controller.SendBulk(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendBulk_ResponseContainsServiceResults()
    {
        var dto = new BulkEmailDto { To = new List<string> { "a@test.com" }, Subject = "S", Body = "B" };
        var results = new List<NotificationResponseDto> { BuildResponse("b1"), BuildResponse("b2") };
        _emailService.Setup(s => s.SendBulkAsync(It.IsAny<BulkEmailDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendBulk(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendBulk_CallsServiceWithDto()
    {
        var dto = new BulkEmailDto { To = new List<string> { "a@test.com" }, Subject = "S", Body = "B", IsHtml = true };
        _emailService.Setup(s => s.SendBulkAsync(It.IsAny<BulkEmailDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        await _controller.SendBulk(dto);

        _emailService.Verify(s => s.SendBulkAsync(dto), Times.Once);
    }

    [Fact]
    public async Task SendBulk_ReturnsServiceResultsInData()
    {
        var dto = new BulkEmailDto { To = new List<string> { "a@test.com" }, Subject = "S", Body = "B" };
        var results = new List<NotificationResponseDto> { BuildResponse("b9") };
        _emailService.Setup(s => s.SendBulkAsync(It.IsAny<BulkEmailDto>())).ReturnsAsync(results);

        var ok = (OkObjectResult)await _controller.SendBulk(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<List<NotificationResponseDto>>>().Subject;
        wrapper.Data.Should().BeSameAs(results);
    }

    [Fact]
    public async Task SendBulk_ReturnsOkStatusCode()
    {
        var dto = new BulkEmailDto { To = new List<string> { "a@test.com" }, Subject = "S", Body = "B" };
        _emailService.Setup(s => s.SendBulkAsync(It.IsAny<BulkEmailDto>())).ReturnsAsync(new List<NotificationResponseDto>());

        var ok = (OkObjectResult)await _controller.SendBulk(dto);

        ok.StatusCode.Should().Be(200);
    }
}
