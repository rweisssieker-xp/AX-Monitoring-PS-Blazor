using Xunit;
using FluentAssertions;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AXMonitoringBU.Api.Tests.Services;

public class KpiDataServiceTests
{
    private readonly AXDbContext _context;
    private readonly Mock<ILogger<KpiDataService>> _loggerMock;
    private readonly KpiDataService _service;

    public KpiDataServiceTests()
    {
        var options = new DbContextOptionsBuilder<AXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AXDbContext(options);
        _loggerMock = new Mock<ILogger<KpiDataService>>();
        _service = new KpiDataService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task GetKpiDataAsync_ShouldReturnDictionaryWithKpiMetrics()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _service.GetKpiDataAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("batch_backlog");
        result.Should().ContainKey("error_rate");
        result.Should().ContainKey("active_sessions");
        result.Should().ContainKey("blocking_chains");
    }

    [Fact]
    public async Task GetSqlHealthAsync_ShouldReturnLatestSqlHealthMetrics()
    {
        // Arrange
        await SeedSqlHealthDataAsync();

        // Act
        var result = await _service.GetSqlHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("cpu_usage");
        result.Should().ContainKey("memory_usage");
        result.Should().ContainKey("active_connections");
    }

    [Fact]
    public async Task GetSqlHealthAsync_WithNoData_ShouldReturnDefaultValues()
    {
        // Act
        var result = await _service.GetSqlHealthAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("cpu_usage");
        result["cpu_usage"].Should().Be(0.0);
    }

    private async Task SeedTestDataAsync()
    {
        var batchJobs = new List<AXMonitoringBU.Api.Models.BatchJob>
        {
            new AXMonitoringBU.Api.Models.BatchJob
            {
                BatchJobId = "BJ001",
                Name = "Test Job 1",
                Status = "Waiting",
                CreatedAt = DateTime.UtcNow
            },
            new AXMonitoringBU.Api.Models.BatchJob
            {
                BatchJobId = "BJ002",
                Name = "Test Job 2",
                Status = "Error",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.BatchJobs.AddRange(batchJobs);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSqlHealthDataAsync()
    {
        var sqlHealth = new AXMonitoringBU.Api.Models.SqlHealth
        {
            CpuUsage = 75.5,
            MemoryUsage = 60.2,
            IoWait = 10.0,
            TempDbUsage = 30.0,
            ActiveConnections = 50,
            LongestRunningQueryMinutes = 5,
            RecordedAt = DateTime.UtcNow
        };

        _context.SqlHealthRecords.Add(sqlHealth);
        await _context.SaveChangesAsync();
    }
}

