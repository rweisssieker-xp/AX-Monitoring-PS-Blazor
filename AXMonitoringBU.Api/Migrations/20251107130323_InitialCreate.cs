using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AXMonitoringBU.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlertId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Baselines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MetricName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    MetricType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MetricClass = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Percentile50 = table.Column<double>(type: "REAL", nullable: false),
                    Percentile95 = table.Column<double>(type: "REAL", nullable: false),
                    Percentile99 = table.Column<double>(type: "REAL", nullable: false),
                    Mean = table.Column<double>(type: "REAL", nullable: false),
                    StandardDeviation = table.Column<double>(type: "REAL", nullable: false),
                    SampleCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BaselineDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baselines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchJobHistoryAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Caption = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ErrorCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorSeverity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ErrorAnalysis = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ErrorSuggestions = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchJobHistoryAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatchJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchJobId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedDuration = table.Column<int>(type: "INTEGER", nullable: true),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    AosServer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlockingChains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockingSessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BlockedSessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BlockingType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Resource = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    SqlText = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockingChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceWindows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DayOfWeek = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    SuppressAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    SuppressNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    P95ThresholdMs = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemediationExecutions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RuleId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TriggerData = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ActionsExecuted = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ResultData = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemediationExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemediationRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TriggerConditions = table.Column<string>(type: "TEXT", nullable: false),
                    Actions = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "INTEGER", nullable: false),
                    BusinessImpact = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemediationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ReportType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Schedule = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CronExpression = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Recipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRun = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextRun = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AosServer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Database = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SqlHealthRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CpuUsage = table.Column<double>(type: "REAL", nullable: false),
                    MemoryUsage = table.Column<double>(type: "REAL", nullable: false),
                    IoWait = table.Column<double>(type: "REAL", nullable: false),
                    TempDbUsage = table.Column<double>(type: "REAL", nullable: false),
                    ActiveConnections = table.Column<int>(type: "INTEGER", nullable: false),
                    LongestRunningQueryMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlHealthRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertId",
                table: "Alerts",
                column: "AlertId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Baselines_BaselineDate",
                table: "Baselines",
                column: "BaselineDate");

            migrationBuilder.CreateIndex(
                name: "IX_Baselines_MetricName_MetricType_MetricClass_Environment",
                table: "Baselines",
                columns: new[] { "MetricName", "MetricType", "MetricClass", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobHistoryAnalyses_AnalyzedAt",
                table: "BatchJobHistoryAnalyses",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobHistoryAnalyses_Caption_CreatedDateTime",
                table: "BatchJobHistoryAnalyses",
                columns: new[] { "Caption", "CreatedDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BatchJobs_BatchJobId",
                table: "BatchJobs",
                column: "BatchJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWindows_Enabled",
                table: "MaintenanceWindows",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWindows_EndTime",
                table: "MaintenanceWindows",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceWindows_StartTime",
                table: "MaintenanceWindows",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceBudgets_Endpoint",
                table: "PerformanceBudgets",
                column: "Endpoint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemediationExecutions_RuleId",
                table: "RemediationExecutions",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RemediationExecutions_StartTime",
                table: "RemediationExecutions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_RemediationRules_Enabled",
                table: "RemediationRules",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_Enabled",
                table: "ScheduledReports",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReports_NextRun",
                table: "ScheduledReports",
                column: "NextRun");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionId",
                table: "Sessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SqlHealthRecords_RecordedAt",
                table: "SqlHealthRecords",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Baselines");

            migrationBuilder.DropTable(
                name: "BatchJobHistoryAnalyses");

            migrationBuilder.DropTable(
                name: "BatchJobs");

            migrationBuilder.DropTable(
                name: "BlockingChains");

            migrationBuilder.DropTable(
                name: "MaintenanceWindows");

            migrationBuilder.DropTable(
                name: "PerformanceBudgets");

            migrationBuilder.DropTable(
                name: "RemediationExecutions");

            migrationBuilder.DropTable(
                name: "RemediationRules");

            migrationBuilder.DropTable(
                name: "ScheduledReports");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "SqlHealthRecords");
        }
    }
}
