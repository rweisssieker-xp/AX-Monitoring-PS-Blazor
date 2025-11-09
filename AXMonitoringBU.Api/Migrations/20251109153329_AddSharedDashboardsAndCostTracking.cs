using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AXMonitoringBU.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedDashboardsAndCostTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Period = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AlertThresholdPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostOptimizationRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecommendationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EstimatedSavings = table.Column<decimal>(type: "TEXT", nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsImplemented = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImplementedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImplementedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostOptimizationRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostTrackings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ResourceName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Cost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    CostBreakdown = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostTrackings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DashboardId = table.Column<int>(type: "INTEGER", nullable: false),
                    SharedWith = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Permission = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SharedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SharedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedDashboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DashboardId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LayoutJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsTeamWorkspace = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedDashboards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostBudgets_IsActive",
                table: "CostBudgets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CostBudgets_Period",
                table: "CostBudgets",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_CostOptimizationRecommendations_CreatedAt",
                table: "CostOptimizationRecommendations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CostOptimizationRecommendations_IsImplemented",
                table: "CostOptimizationRecommendations",
                column: "IsImplemented");

            migrationBuilder.CreateIndex(
                name: "IX_CostOptimizationRecommendations_Priority",
                table: "CostOptimizationRecommendations",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CostOptimizationRecommendations_ResourceType_ResourceId",
                table: "CostOptimizationRecommendations",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackings_CreatedAt",
                table: "CostTrackings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackings_PeriodEnd",
                table: "CostTrackings",
                column: "PeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackings_PeriodStart",
                table: "CostTrackings",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_CostTrackings_ResourceType_ResourceId",
                table: "CostTrackings",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardShares_DashboardId",
                table: "DashboardShares",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardShares_DashboardId_SharedWith",
                table: "DashboardShares",
                columns: new[] { "DashboardId", "SharedWith" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardShares_SharedWith",
                table: "DashboardShares",
                column: "SharedWith");

            migrationBuilder.CreateIndex(
                name: "IX_SharedDashboards_CreatedBy",
                table: "SharedDashboards",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SharedDashboards_DashboardId",
                table: "SharedDashboards",
                column: "DashboardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedDashboards_IsPublic",
                table: "SharedDashboards",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SharedDashboards_IsTeamWorkspace",
                table: "SharedDashboards",
                column: "IsTeamWorkspace");

            migrationBuilder.CreateIndex(
                name: "IX_SharedDashboards_TeamName",
                table: "SharedDashboards",
                column: "TeamName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostBudgets");

            migrationBuilder.DropTable(
                name: "CostOptimizationRecommendations");

            migrationBuilder.DropTable(
                name: "CostTrackings");

            migrationBuilder.DropTable(
                name: "DashboardShares");

            migrationBuilder.DropTable(
                name: "SharedDashboards");
        }
    }
}
