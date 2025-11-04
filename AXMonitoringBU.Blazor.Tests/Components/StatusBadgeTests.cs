using Xunit;
using FluentAssertions;
using Bunit;
using AXMonitoringBU.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AXMonitoringBU.Blazor.Services;

namespace AXMonitoringBU.Blazor.Tests.Components;

public class StatusBadgeTests : TestContext
{
    [Fact]
    public void StatusBadge_ShouldRenderBadge()
    {
        // Arrange
        var status = "Running";

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain("badge");
    }

    [Fact]
    public void StatusBadge_WithRunningStatus_ShouldHaveSuccessClass()
    {
        // Arrange
        var status = "Running";

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain("bg-success");
    }

    [Fact]
    public void StatusBadge_WithErrorStatus_ShouldHaveDangerClass()
    {
        // Arrange
        var status = "Error";

        // Act
        var cut = RenderComponent<StatusBadge>(parameters => parameters
            .Add(p => p.Status, status));

        // Assert
        cut.Markup.Should().Contain("bg-danger");
    }
}

