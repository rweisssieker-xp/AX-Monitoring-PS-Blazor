using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AXMonitoringBU.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsAndPerformanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessImpacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BusinessProcess = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ImpactScore = table.Column<double>(type: "REAL", nullable: false),
                    AffectedUsers = table.Column<int>(type: "INTEGER", nullable: false),
                    FinancialImpactPerHour = table.Column<decimal>(type: "TEXT", nullable: true),
                    SlaMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    EscalationThresholdMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    EscalationContact = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    IsComplianceCritical = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessImpacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorCorrelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobNameA = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    JobNameB = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CorrelationCoefficient = table.Column<double>(type: "REAL", nullable: false),
                    CoOccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    JobAFailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    JobBFailureCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeWindowMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDependency = table.Column<bool>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorCorrelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Format = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Fields = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobBaselines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    BaselineDurationP50 = table.Column<double>(type: "REAL", nullable: false),
                    BaselineDurationP90 = table.Column<double>(type: "REAL", nullable: false),
                    BaselineDurationP95 = table.Column<double>(type: "REAL", nullable: false),
                    BaselineCpuUsage = table.Column<double>(type: "REAL", nullable: true),
                    BaselineMemoryUsage = table.Column<double>(type: "REAL", nullable: true),
                    BaselineRecordsProcessed = table.Column<long>(type: "INTEGER", nullable: true),
                    HistoricalErrorRate = table.Column<double>(type: "REAL", nullable: false),
                    SampleSize = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobBaselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobExecutionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchJobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    JobName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AosServer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CpuUsage = table.Column<double>(type: "REAL", nullable: true),
                    MemoryUsage = table.Column<double>(type: "REAL", nullable: true),
                    QueryCount = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordsProcessed = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Secret = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationSettings_Key",
                table: "ApplicationSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImpacts_ImpactScore",
                table: "BusinessImpacts",
                column: "ImpactScore");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImpacts_JobName",
                table: "BusinessImpacts",
                column: "JobName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessImpacts_Priority",
                table: "BusinessImpacts",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCorrelations_CalculatedAt",
                table: "ErrorCorrelations",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCorrelations_CorrelationCoefficient",
                table: "ErrorCorrelations",
                column: "CorrelationCoefficient");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorCorrelations_JobNameA_JobNameB",
                table: "ErrorCorrelations",
                columns: new[] { "JobNameA", "JobNameB" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportTemplates_EntityType_Format",
                table: "ExportTemplates",
                columns: new[] { "EntityType", "Format" });

            migrationBuilder.CreateIndex(
                name: "IX_JobBaselines_CalculatedAt",
                table: "JobBaselines",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobBaselines_JobName",
                table: "JobBaselines",
                column: "JobName");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionHistories_BatchJobId",
                table: "JobExecutionHistories",
                column: "BatchJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionHistories_JobName",
                table: "JobExecutionHistories",
                column: "JobName");

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionHistories_JobName_StartTime",
                table: "JobExecutionHistories",
                columns: new[] { "JobName", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutionHistories_StartTime",
                table: "JobExecutionHistories",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_Enabled",
                table: "WebhookSubscriptions",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_EventType",
                table: "WebhookSubscriptions",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "BusinessImpacts");

            migrationBuilder.DropTable(
                name: "ErrorCorrelations");

            migrationBuilder.DropTable(
                name: "ExportTemplates");

            migrationBuilder.DropTable(
                name: "JobBaselines");

            migrationBuilder.DropTable(
                name: "JobExecutionHistories");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");
        }
    }
}
