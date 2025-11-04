using Xunit;
using FluentAssertions;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Tests.Services;

public class AlertServiceTests
{
    private readonly AXDbContext _context;
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        var options = new DbContextOptionsBuilder<AXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AXDbContext(options);
        _loggerMock = new Mock<ILogger<AlertService>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _service = new AlertService(_context, _loggerMock.Object, _serviceProviderMock.Object);
    }

    [Fact]
    public async Task CreateAlertAsync_ShouldCreateNewAlert()
    {
        // Arrange
        var type = "HighCPU";
        var severity = "Warning";
        var message = "CPU usage is above threshold";

        // Act
        var result = await _service.CreateAlertAsync(type, severity, message);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(type);
        result.Severity.Should().Be(severity);
        result.Message.Should().Be(message);
        result.Status.Should().Be("Active");
        result.AlertId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAlertsAsync_ShouldReturnAllAlerts()
    {
        // Arrange
        await SeedAlertsAsync();

        // Act
        var result = await _service.GetAlertsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAlertsAsync_WithStatusFilter_ShouldReturnFilteredAlerts()
    {
        // Arrange
        await SeedAlertsAsync();

        // Act
        var result = await _service.GetAlertsAsync("Active");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Status.Should().Be("Active");
    }

    [Fact]
    public async Task UpdateAlertStatusAsync_ShouldUpdateAlertStatus()
    {
        // Arrange
        var alert = await CreateTestAlertAsync();

        // Act
        var result = await _service.UpdateAlertStatusAsync(alert.Id, "Resolved");

        // Assert
        result.Should().BeTrue();
        var updatedAlert = await _service.GetAlertByIdAsync(alert.Id);
        updatedAlert.Should().NotBeNull();
        updatedAlert!.Status.Should().Be("Resolved");
        updatedAlert.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAlertAsync_ShouldDeleteAlert()
    {
        // Arrange
        var alert = await CreateTestAlertAsync();

        // Act
        var result = await _service.DeleteAlertAsync(alert.Id);

        // Assert
        result.Should().BeTrue();
        var deletedAlert = await _service.GetAlertByIdAsync(alert.Id);
        deletedAlert.Should().BeNull();
    }

    private async Task<Alert> CreateTestAlertAsync()
    {
        var alert = await _service.CreateAlertAsync("Test", "Info", "Test alert");
        return alert;
    }

    private async Task SeedAlertsAsync()
    {
        var alerts = new List<Alert>
        {
            new Alert
            {
                AlertId = "ALERT_001",
                Type = "HighCPU",
                Severity = "Warning",
                Message = "CPU usage high",
                Status = "Active",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            },
            new Alert
            {
                AlertId = "ALERT_002",
                Type = "Memory",
                Severity = "Critical",
                Message = "Memory usage critical",
                Status = "Resolved",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();
    }
}

