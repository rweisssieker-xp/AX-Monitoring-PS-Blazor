using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Controllers;
using AXMonitoringBU.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AXMonitoringBU.Api.Tests.Controllers;

public class MetricsControllerTests
{
    private readonly Mock<IKpiDataService> _kpiDataServiceMock;
    private readonly Mock<IBusinessKpiService> _businessKpiServiceMock;
    private readonly Mock<ILogger<MetricsController>> _loggerMock;
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _kpiDataServiceMock = new Mock<IKpiDataService>();
        _businessKpiServiceMock = new Mock<IBusinessKpiService>();
        _loggerMock = new Mock<ILogger<MetricsController>>();
        _controller = new MetricsController(
            _kpiDataServiceMock.Object,
            _businessKpiServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetCurrentMetrics_ShouldReturnOkWithMetrics()
    {
        // Arrange
        var kpiData = new Dictionary<string, object>
        {
            { "batch_backlog", 5 },
            { "error_rate", 2.5 }
        };

        var sqlHealth = new Dictionary<string, object>
        {
            { "cpu_usage", 75.0 },
            { "memory_usage", 60.0 }
        };

        _kpiDataServiceMock.Setup(x => x.GetKpiDataAsync())
            .ReturnsAsync(kpiData);
        _kpiDataServiceMock.Setup(x => x.GetSqlHealthAsync())
            .ReturnsAsync(sqlHealth);

        // Act
        var result = await _controller.GetCurrentMetrics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCurrentMetrics_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        _kpiDataServiceMock.Setup(x => x.GetKpiDataAsync())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetCurrentMetrics();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}

