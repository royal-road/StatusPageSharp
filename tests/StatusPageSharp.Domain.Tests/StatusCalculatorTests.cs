using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Domain.Logic;

namespace StatusPageSharp.Domain.Tests;

public class StatusCalculatorTests
{
    [Fact]
    public void CalculateServiceStatus_ReturnsMajorOutage_WhenMajorImpactExists()
    {
        var status = StatusCalculator.CalculateServiceStatus(
            isUnderMaintenance: true,
            activeImpacts: [IncidentImpactLevel.PartialOutage, IncidentImpactLevel.MajorOutage]
        );

        Assert.Equal(ServiceStatus.MajorOutage, status);
    }

    [Fact]
    public void CalculateServiceStatus_ReturnsPartialOutage_WhenOnlyPartialImpactExists()
    {
        var status = StatusCalculator.CalculateServiceStatus(
            isUnderMaintenance: false,
            activeImpacts: [IncidentImpactLevel.PartialOutage]
        );

        Assert.Equal(ServiceStatus.PartialOutage, status);
    }

    [Fact]
    public void CalculateServiceStatus_ReturnsUnderMaintenance_WhenNoActiveImpactExists()
    {
        var status = StatusCalculator.CalculateServiceStatus(
            isUnderMaintenance: true,
            activeImpacts: []
        );

        Assert.Equal(ServiceStatus.UnderMaintenance, status);
    }

    [Fact]
    public void CalculateGroupStatus_ReturnsMajorOutage_WhenAllNonMaintenanceServicesAreDown()
    {
        var status = StatusCalculator.CalculateGroupStatus([
            ServiceStatus.MajorOutage,
            ServiceStatus.MajorOutage,
        ]);

        Assert.Equal(ServiceStatus.MajorOutage, status);
    }

    [Fact]
    public void CalculateGroupStatus_ReturnsPartialOutage_WhenAtLeastOneServiceIsDegraded()
    {
        var status = StatusCalculator.CalculateGroupStatus([
            ServiceStatus.Operational,
            ServiceStatus.PartialOutage,
            ServiceStatus.UnderMaintenance,
        ]);

        Assert.Equal(ServiceStatus.PartialOutage, status);
    }

    [Fact]
    public void CalculateGroupStatus_ReturnsUnderMaintenance_WhenOnlyMaintenanceStatusesExist()
    {
        var status = StatusCalculator.CalculateGroupStatus([
            ServiceStatus.UnderMaintenance,
            ServiceStatus.UnderMaintenance,
        ]);

        Assert.Equal(ServiceStatus.UnderMaintenance, status);
    }
}
