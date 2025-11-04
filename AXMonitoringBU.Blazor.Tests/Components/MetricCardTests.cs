using Xunit;
using FluentAssertions;
using Bunit;
using AXMonitoringBU.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Tests.Components;

public class MetricCardTests : TestContext
{
    public MetricCardTests()
    {
        Services.AddSingleton(Mock.Of<IMetricsService>());
    }

    [Fact]
    public void MetricCard_ShouldRenderTitle()
    {
        // Arrange
        var title = "Test Metric";
        var value = "100";

        // Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Value, value));

        // Assert
        cut.Markup.Should().Contain(title);
        cut.Markup.Should().Contain(value);
    }

    [Fact]
    public void MetricCard_ShouldRenderValue()
    {
        // Arrange
        var title = "Test Metric";
        var value = "100";

        // Act
        var cut = RenderComponent<MetricCard>(parameters => parameters
            .Add(p => p.Title, title)
            .Add(p => p.Value, value));

        // Assert
        cut.Markup.Should().Contain(value);
    }
}

