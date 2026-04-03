using System.ComponentModel.DataAnnotations;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Web.Tests.Pages.Admin;

public class ServiceUpsertModelValidationTests
{
    [Fact]
    public void Validate_ReturnsError_WhenIcmpMonitorHasNoHost()
    {
        var model = CreateModel();
        model.MonitorType = MonitorType.Icmp;
        model.Host = null;

        var validationResults = Validate(model);

        Assert.Contains(
            validationResults,
            result => result.ErrorMessage == "ICMP monitors require a host."
        );
    }

    [Fact]
    public void Validate_DoesNotRequirePort_WhenIcmpMonitorHasHost()
    {
        var model = CreateModel();
        model.MonitorType = MonitorType.Icmp;
        model.Host = "status.example.com";
        model.Port = null;

        var validationResults = Validate(model);

        Assert.DoesNotContain(
            validationResults,
            result => result.MemberNames.Contains(nameof(ServiceUpsertModel.Port))
        );
    }

    [Fact]
    public void Validate_ReturnsError_WhenHttpMonitorHasNoUrl()
    {
        var model = CreateModel();
        model.MonitorType = MonitorType.Http;
        model.Url = null;

        var validationResults = Validate(model);

        Assert.Contains(
            validationResults,
            result => result.ErrorMessage == "HTTP and HTTPS monitors require a URL."
        );
    }

    private static ServiceUpsertModel CreateModel() =>
        new()
        {
            ServiceGroupId = Guid.NewGuid(),
            Name = "API",
            Slug = "api",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
            FailureThreshold = 3,
            RecoveryThreshold = 3,
            MonitorType = MonitorType.Https,
            Url = "https://status.example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200-299",
            TimeoutSeconds = 10,
        };

    private static IReadOnlyList<ValidationResult> Validate(ServiceUpsertModel model)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true
        );
        return validationResults;
    }
}
