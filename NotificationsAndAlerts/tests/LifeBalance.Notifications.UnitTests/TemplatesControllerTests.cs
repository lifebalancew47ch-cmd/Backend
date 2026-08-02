using FluentAssertions;
using LifeBalance.Notifications.Application.DTOs;
using LifeBalance.Notifications.Application.Interfaces;
using LifeBalance.Notifications.Domain.Enums;
using LifeBalance.Notifications.Presentation.Controllers;
using LifeBalance.Notifications.Shared.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeBalance.Notifications.UnitTests;

public class TemplatesControllerTests
{
    private readonly Mock<ITemplateService> _templateService;
    private readonly TemplatesController _controller;

    public TemplatesControllerTests()
    {
        _templateService = new Mock<ITemplateService>();
        _controller = new TemplatesController(_templateService.Object);
    }

    private static TemplateDto BuildTemplate(string id = "tpl-1") => new()
    {
        Id = id,
        Name = "Name",
        Subject = "Subject",
        BodyContent = "Body",
        Type = NotificationType.Information,
        Channel = NotificationChannel.Email,
        IsGlobal = true
    };

    private static CreateTemplateDto BuildCreateDto() => new()
    {
        Name = "Name",
        Subject = "Subject",
        BodyContent = "Body",
        Type = NotificationType.Information,
        Channel = NotificationChannel.Email
    };

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkObjectResult()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(BuildTemplate());

        var result = await _controller.Create(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_ResponseWrapsServiceResult()
    {
        var dto = BuildCreateDto();
        var template = BuildTemplate("tpl-5");
        _templateService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(template);

        var ok = (OkObjectResult)await _controller.Create(dto);

        ok.StatusCode.Should().Be(200);
        var wrapper = ok.Value.Should().BeOfType<Response<TemplateDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().BeSameAs(template);
    }

    [Fact]
    public async Task Create_CallsServiceWithDto()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.CreateAsync(It.IsAny<CreateTemplateDto>())).ReturnsAsync(BuildTemplate());

        await _controller.Create(dto);

        _templateService.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsServiceResultInData()
    {
        var dto = BuildCreateDto();
        var template = BuildTemplate("tpl-42");
        _templateService.Setup(s => s.CreateAsync(It.IsAny<CreateTemplateDto>())).ReturnsAsync(template);

        var ok = (OkObjectResult)await _controller.Create(dto);

        var wrapper = ok.Value.Should().BeOfType<Response<TemplateDto>>().Subject;
        wrapper.Data.Should().BeSameAs(template);
    }

    [Fact]
    public async Task Create_ReturnsOkStatusCode()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.CreateAsync(It.IsAny<CreateTemplateDto>())).ReturnsAsync(BuildTemplate());

        var ok = (OkObjectResult)await _controller.Create(dto);

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        _templateService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<TemplateDto> { BuildTemplate() });

        var result = await _controller.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_ResponseContainsServiceResults()
    {
        var templates = new List<TemplateDto> { BuildTemplate("t1"), BuildTemplate("t2") };
        _templateService.Setup(s => s.GetAllAsync()).ReturnsAsync(templates);

        var ok = (OkObjectResult)await _controller.GetAll();

        var wrapper = ok.Value.Should().BeOfType<Response<List<TemplateDto>>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(templates);
    }

    [Fact]
    public async Task GetAll_CallsService()
    {
        _templateService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<TemplateDto>());

        await _controller.GetAll();

        _templateService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenNoTemplates_ReturnsEmptyData()
    {
        _templateService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<TemplateDto>());

        var ok = (OkObjectResult)await _controller.GetAll();

        var wrapper = ok.Value.Should().BeOfType<Response<List<TemplateDto>>>().Subject;
        wrapper.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ReturnsOkStatusCode()
    {
        _templateService.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<TemplateDto>());

        var ok = (OkObjectResult)await _controller.GetAll();

        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_WhenTemplateExists_ReturnsOkObjectResult()
    {
        _templateService.Setup(s => s.GetByIdAsync("tpl-1")).ReturnsAsync(BuildTemplate());

        var result = await _controller.GetById("tpl-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenTemplateExists_ResponseWrapsServiceResult()
    {
        var template = BuildTemplate("tpl-3");
        _templateService.Setup(s => s.GetByIdAsync("tpl-3")).ReturnsAsync(template);

        var ok = (OkObjectResult)await _controller.GetById("tpl-3");

        var wrapper = ok.Value.Should().BeOfType<Response<TemplateDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(template);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFoundObjectResult()
    {
        _templateService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((TemplateDto?)null);

        var result = await _controller.GetById("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_WhenNotFound_ResponseMessageIsTemplateNotFound()
    {
        _templateService.Setup(s => s.GetByIdAsync("missing")).ReturnsAsync((TemplateDto?)null);

        var nf = (NotFoundObjectResult)await _controller.GetById("missing");

        nf.StatusCode.Should().Be(404);
        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Template not found");
    }

    [Fact]
    public async Task GetById_CallsServiceWithGivenId()
    {
        _templateService.Setup(s => s.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(BuildTemplate());

        await _controller.GetById("tpl-9");

        _templateService.Verify(s => s.GetByIdAsync("tpl-9"), Times.Once);
    }

    [Fact]
    public async Task Update_WhenTemplateExists_ReturnsOkObjectResult()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.UpdateAsync("tpl-1", dto)).ReturnsAsync(BuildTemplate());

        var result = await _controller.Update("tpl-1", dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_WhenTemplateExists_ResponseWrapsServiceResult()
    {
        var dto = BuildCreateDto();
        var template = BuildTemplate("tpl-6");
        _templateService.Setup(s => s.UpdateAsync("tpl-6", dto)).ReturnsAsync(template);

        var ok = (OkObjectResult)await _controller.Update("tpl-6", dto);

        var wrapper = ok.Value.Should().BeOfType<Response<TemplateDto>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Data.Should().BeSameAs(template);
    }

    [Fact]
    public async Task Update_WhenNotFound_ReturnsNotFoundObjectResult()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.UpdateAsync("missing", dto)).ReturnsAsync((TemplateDto?)null);

        var result = await _controller.Update("missing", dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_WhenNotFound_ResponseMessageIsTemplateNotFound()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.UpdateAsync("missing", dto)).ReturnsAsync((TemplateDto?)null);

        var nf = (NotFoundObjectResult)await _controller.Update("missing", dto);

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Template not found");
    }

    [Fact]
    public async Task Update_CallsServiceWithIdAndDto()
    {
        var dto = BuildCreateDto();
        _templateService.Setup(s => s.UpdateAsync(It.IsAny<string>(), It.IsAny<CreateTemplateDto>())).ReturnsAsync(BuildTemplate());

        await _controller.Update("tpl-8", dto);

        _templateService.Verify(s => s.UpdateAsync("tpl-8", dto), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsTrue_ReturnsOkObjectResult()
    {
        _templateService.Setup(s => s.DeleteAsync("tpl-1")).ReturnsAsync(true);

        var result = await _controller.Delete("tpl-1");

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsTrue_ReturnsSuccessResponse()
    {
        _templateService.Setup(s => s.DeleteAsync("tpl-1")).ReturnsAsync(true);

        var ok = (OkObjectResult)await _controller.Delete("tpl-1");

        var wrapper = ok.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeTrue();
        wrapper.Message.Should().Be("Success");
        wrapper.Data.Should().Be("Template deleted");
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsFalse_ReturnsNotFoundObjectResult()
    {
        _templateService.Setup(s => s.DeleteAsync("missing")).ReturnsAsync(false);

        var result = await _controller.Delete("missing");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsFalse_ResponseMessageIsTemplateNotFound()
    {
        _templateService.Setup(s => s.DeleteAsync("missing")).ReturnsAsync(false);

        var nf = (NotFoundObjectResult)await _controller.Delete("missing");

        var wrapper = nf.Value.Should().BeOfType<Response<string>>().Subject;
        wrapper.Success.Should().BeFalse();
        wrapper.Message.Should().Be("Template not found");
    }

    [Fact]
    public async Task Delete_CallsServiceWithGivenId()
    {
        _templateService.Setup(s => s.DeleteAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _controller.Delete("tpl-2");

        _templateService.Verify(s => s.DeleteAsync("tpl-2"), Times.Once);
    }
}
