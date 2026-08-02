using FluentAssertions;
using FluentValidation;
using LifeBalance.Administration.Application.Features.Settings;
using LifeBalance.Administration.Application.Interfaces;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Interfaces;
using Moq;

namespace LifeBalance.Administration.UnitTests.Features;

public class SettingsFeaturesTests
{
    private readonly Mock<IRepository<SystemConfiguration>> _systemRepo = new();
    private readonly Mock<IRepository<GlobalConfiguration>> _globalRepo = new();
    private readonly Mock<ICacheService> _cache = new();

    private SettingsCommandHandler CreateCommandHandler()
        => new(_systemRepo.Object, _globalRepo.Object, _cache.Object);

    private SettingsQueryHandler CreateQueryHandler()
        => new(_systemRepo.Object, _globalRepo.Object, _cache.Object);

    private static void SetupExistingConfigs(
        Mock<IRepository<SystemConfiguration>> systemRepo,
        Mock<IRepository<GlobalConfiguration>> globalRepo)
    {
        systemRepo.Setup(r => r.GetByIdAsync(SystemConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SystemConfiguration.CreateDefaults());
        globalRepo.Setup(r => r.GetByIdAsync(GlobalConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GlobalConfiguration.CreateDefaults());
    }

    [Fact]
    public void Validator_ValidRequest_Passes()
    {
        var validator = new UpdateSettingsCommandValidator();
        var request = new UpdateSettingsRequest(
            new SystemSettingsDto(
                Sedentary: new SedentarySettingsDto(90, 5),
                Sync: new SyncSettingsDto(15),
                Ai: null,
                Dashboard: null,
                Reports: null,
                Alerts: null,
                Email: null,
                Push: null,
                Notifications: null,
                Saas: null,
                Rules: null),
            GlobalConfig: null);

        var result = validator.Validate(new UpdateSettingsCommand(request));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_OutOfRangeSedentary_Fails()
    {
        var validator = new UpdateSettingsCommandValidator();
        var request = new UpdateSettingsRequest(
            new SystemSettingsDto(new SedentarySettingsDto(1000, 5), null, null, null, null, null, null, null, null, null, null),
            null);

        var result = validator.Validate(new UpdateSettingsCommand(request));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("MaxSedentaryMinutes"));
    }

    [Fact]
    public void Validator_InvalidPredictionUrl_Fails()
    {
        var validator = new UpdateSettingsCommandValidator();
        var request = new UpdateSettingsRequest(
            new SystemSettingsDto(null, null, new AiSettingsDto(true, "not-a-url", 30, 0.8, 90), null, null, null, null, null, null, null, null),
            null);

        var result = validator.Validate(new UpdateSettingsCommand(request));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CommandHandler_Update_PersistsAndInvalidatesCache()
    {
        SetupExistingConfigs(_systemRepo, _globalRepo);
        var handler = CreateCommandHandler();
        var request = new UpdateSettingsRequest(
            new SystemSettingsDto(new SedentarySettingsDto(60, 5), null, null, null, null, null, null, null, null, null, null),
            null);

        var result = await handler.Handle(new UpdateSettingsCommand(request, "admin-1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _systemRepo.Verify(r => r.UpdateAsync(It.IsAny<SystemConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SettingsCommandHandler.SettingsCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommandHandler_Update_CreatesMissingConfigs()
    {
        _systemRepo.Setup(r => r.GetByIdAsync(SystemConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);
        _globalRepo.Setup(r => r.GetByIdAsync(GlobalConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GlobalConfiguration?)null);

        var handler = CreateCommandHandler();
        var result = await handler.Handle(new UpdateSettingsCommand(new UpdateSettingsRequest(null, null), "admin-1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        _systemRepo.Verify(r => r.AddAsync(It.IsAny<SystemConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _globalRepo.Verify(r => r.AddAsync(It.IsAny<GlobalConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueryHandler_ReturnsCachedValueWhenPresent()
    {
        var cached = new SettingsDto("1", new SystemSettingsDto(null, null, null, null, null, null, null, null, null, null, null),
            new GlobalSettingsDto("LifeBalance", null, null, "es", "UTC", 50, 60, null), "system", null);
        _cache.Setup(c => c.GetAsync<SettingsDto>(SettingsCommandHandler.SettingsCacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetSettingsQuery(), CancellationToken.None);

        result.Data.Should().Be(cached);
        _systemRepo.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task QueryHandler_CreatesDefaultsWhenMissing()
    {
        _systemRepo.Setup(r => r.GetByIdAsync(SystemConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SystemConfiguration?)null);
        _globalRepo.Setup(r => r.GetByIdAsync(GlobalConfiguration.SingletonId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GlobalConfiguration?)null);

        var handler = CreateQueryHandler();
        var result = await handler.Handle(new GetSettingsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _systemRepo.Verify(r => r.AddAsync(It.IsAny<SystemConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetAsync(SettingsCommandHandler.SettingsCacheKey, It.IsAny<SettingsDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommandHandler_Reset_RestoresDefaults()
    {
        SetupExistingConfigs(_systemRepo, _globalRepo);
        var handler = CreateCommandHandler();

        var result = await handler.Handle(new ResetSettingsCommand("admin-1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        _systemRepo.Verify(r => r.UpdateAsync(It.IsAny<SystemConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _globalRepo.Verify(r => r.UpdateAsync(It.IsAny<GlobalConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SettingsCommandHandler.SettingsCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
