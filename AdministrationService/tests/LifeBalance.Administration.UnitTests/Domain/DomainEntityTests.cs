using FluentAssertions;
using LifeBalance.Administration.Domain.Entities;
using LifeBalance.Administration.Domain.Enums;

namespace LifeBalance.Administration.UnitTests.Domain;

public class CatalogTests
{
    [Fact]
    public void Constructor_NormalizesAndUppercasesCode()
    {
        var catalog = new Catalog("activity-type ", "Activity Types", "desc", "misc");

        catalog.Code.Should().Be("ACTIVITY-TYPE");
        catalog.Name.Should().Be("Activity Types");
        catalog.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ThrowsWhenCodeOrNameMissing()
    {
        var act = () => new Catalog(" ", "name", "desc", "cat");
        act.Should().Throw<ArgumentException>();

        var act2 = () => new Catalog("code", "", "desc", "cat");
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ReplacesItemsAndTouches()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");
        var versionBefore = catalog.Version;

        catalog.Update("new name", "new desc", "new cat", new List<CatalogItem>
        {
            new CatalogItem { Code = "A", Name = "Alpha" }
        });

        catalog.Name.Should().Be("new name");
        catalog.Category.Should().Be("new cat");
        catalog.Items.Should().ContainSingle(i => i.Code == "A");
        catalog.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void ActivateAndDeactivate_FlipState()
    {
        var catalog = new Catalog("code", "name", "desc", "cat");

        catalog.Deactivate();
        catalog.IsActive.Should().BeFalse();

        catalog.Activate();
        catalog.IsActive.Should().BeTrue();
    }
}

public class SystemParameterTests
{
    [Fact]
    public void Constructor_SetsFieldsAndDefaults()
    {
        var p = new SystemParameter("max-score", "Max Score", "desc", ParameterDataType.Number, "100", "rules", "0", "100", "pts", 3);

        p.Code.Should().Be("max-score");
        p.DataType.Should().Be(ParameterDataType.Number);
        p.MinValue.Should().Be("0");
        p.IsActive.Should().BeTrue();
        p.IsSystem.Should().BeFalse();
        p.Order.Should().Be(3);
    }

    [Fact]
    public void Constructor_RequiresJsonValue()
    {
        var act = () => new SystemParameter("cfg", "Config", "desc", ParameterDataType.Json, "", "cat");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_TouchesVersion()
    {
        var p = new SystemParameter("max-score", "Max Score", "desc", ParameterDataType.Number, "100", "rules");
        var versionBefore = p.Version;

        p.Update("New Max", "new desc", ParameterDataType.Number, "90", "rules", "0", "90", "pts", 1);

        p.Name.Should().Be("New Max");
        p.Value.Should().Be("90");
        p.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void ActivateAndDeactivate_FlipState()
    {
        var p = new SystemParameter("max-score", "Max Score", "desc", ParameterDataType.Number, "100", "rules");

        p.Deactivate();
        p.IsActive.Should().BeFalse();

        p.Activate();
        p.IsActive.Should().BeTrue();
    }
}

public class FeatureFlagTests
{
    [Fact]
    public void Constructor_UppercasesCodeAndDefaultsEnabled()
    {
        var flag = new FeatureFlag("ai-module", "AI Module", "desc", "ai");

        flag.Code.Should().Be("AI-MODULE");
        flag.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void EnableAndDisable_TrackActorAndTimestamps()
    {
        var flag = new FeatureFlag("ai-module", "AI Module", "desc", "ai");

        flag.Disable("admin-1");
        flag.IsEnabled.Should().BeFalse();
        flag.DisabledBy.Should().Be("admin-1");
        flag.DisabledAt.Should().NotBeNull();

        flag.Enable("admin-2");
        flag.IsEnabled.Should().BeTrue();
        flag.EnabledBy.Should().Be("admin-2");
        flag.EnabledAt.Should().NotBeNull();
        flag.DisabledBy.Should().BeNull();
        flag.DisabledAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_ThrowsWhenCodeMissing()
    {
        var act = () => new FeatureFlag("", "AI Module", "desc", "ai");
        act.Should().Throw<ArgumentException>();
    }
}

public class MaintenanceModeTests
{
    [Fact]
    public void CreateDefault_UsesSingletonId()
    {
        var mode = MaintenanceMode.CreateDefault();

        mode.Id.Should().Be(MaintenanceMode.SingletonId);
        mode.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void EnableAndDisable_TrackState()
    {
        var mode = MaintenanceMode.CreateDefault();
        var scheduledEnd = DateTime.UtcNow.AddHours(2);

        mode.Enable("Deploying v2", "admin-1", scheduledEnd);
        mode.IsEnabled.Should().BeTrue();
        mode.Message.Should().Be("Deploying v2");
        mode.ScheduledEndAt.Should().Be(scheduledEnd);
        mode.EnabledBy.Should().Be("admin-1");

        mode.Disable("admin-2");
        mode.IsEnabled.Should().BeFalse();
        mode.DisabledBy.Should().Be("admin-2");
        mode.DisabledAt.Should().NotBeNull();
    }
}

public class ServiceStatusTests
{
    [Fact]
    public void Report_HealthyUpdatesLastSuccessAt()
    {
        var status = new ServiceStatus(MicroserviceName.Auth, "Auth & Profile");
        var checkedAt = DateTime.UtcNow;

        status.Report(ServiceHealthStatus.Healthy, 200, "OK", 42, "1.0.0", null, checkedAt);

        status.Status.Should().Be(ServiceHealthStatus.Healthy);
        status.StatusCode.Should().Be(200);
        status.LatencyMs.Should().Be(42);
        status.ServiceVersion.Should().Be("1.0.0");
        status.LastSuccessAt.Should().Be(checkedAt);
    }

    [Fact]
    public void Report_UnhealthyKeepsLastSuccessAt()
    {
        var status = new ServiceStatus(MicroserviceName.Dashboard, "Dashboard");

        status.Report(ServiceHealthStatus.Healthy, 200, "OK", 42, "1.0.0", null, DateTime.UtcNow.AddMinutes(-5));
        var lastSuccess = status.LastSuccessAt;

        status.Report(ServiceHealthStatus.Unhealthy, 503, "Down", 500, null, null, DateTime.UtcNow);

        status.LastSuccessAt.Should().Be(lastSuccess);
        status.Status.Should().Be(ServiceHealthStatus.Unhealthy);
    }
}

public class SystemConfigurationTests
{
    [Fact]
    public void CreateDefaults_UsesSingletonIdAndActiveState()
    {
        var config = SystemConfiguration.CreateDefaults();

        config.Id.Should().Be(SystemConfiguration.SingletonId);
        config.Sedentary.MaxSedentaryMinutes.Should().Be(90);
        config.Email.SmtpPort.Should().Be(587);
    }

    [Fact]
    public void Apply_NullSectionsFallBackToDefaults()
    {
        var config = SystemConfiguration.CreateDefaults();

        config.Apply(null, null, null, null, null, null, null, null, null, null, null, "admin-1");

        config.Sedentary.MaxSedentaryMinutes.Should().Be(90);
        config.Sync.SyncIntervalMinutes.Should().Be(15);
        config.UpdatedBy.Should().Be("admin-1");
        config.Version.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Apply_ReplacesProvidedSections()
    {
        var config = SystemConfiguration.CreateDefaults();

        config.Apply(new SedentarySettings { MaxSedentaryMinutes = 60 }, null, null, null, null, null,
            null, null, null, null, null, "admin-1");

        config.Sedentary.MaxSedentaryMinutes.Should().Be(60);
        config.Sedentary.MinActiveBreakMinutes.Should().Be(5);
    }

    [Fact]
    public void ResetToDefaults_RestoresOriginalValues()
    {
        var config = SystemConfiguration.CreateDefaults();
        config.Apply(new SedentarySettings { MaxSedentaryMinutes = 10 }, null, null, null, null, null,
            null, null, null, null, null, "admin-1");

        config.ResetToDefaults("system");

        config.Sedentary.MaxSedentaryMinutes.Should().Be(90);
        config.UpdatedBy.Should().Be("system");
    }
}

public class GlobalConfigurationTests
{
    [Fact]
    public void Apply_UsesFallbackNameWhenBlank()
    {
        var config = GlobalConfiguration.CreateDefaults();

        config.Apply(" ", "https://frontend", "support@lb.app", "en", "UTC", 100, 90, null, "admin-1");

        config.ApplicationName.Should().Be("LifeBalance");
        config.FrontendBaseUrl.Should().Be("https://frontend");
        config.MaxUploadSizeMb.Should().Be(100);
    }

    [Fact]
    public void ResetToDefaults_RestoresValues()
    {
        var config = GlobalConfiguration.CreateDefaults();
        config.Apply("Other", "https://x", "x@x.com", "en", "UTC", 999, 120, new Dictionary<string, string> { ["k"] = "v" }, "admin-1");

        config.ResetToDefaults("system");

        config.ApplicationName.Should().Be("LifeBalance");
        config.MaxUploadSizeMb.Should().Be(50);
        config.SessionTimeoutMinutes.Should().Be(60);
    }
}
