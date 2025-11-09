using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AXMonitoringBU.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertEscalationAndCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "Alerts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcknowledgedBy",
                table: "Alerts",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrelationId",
                table: "Alerts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Alerts",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertCorrelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FirstDetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AlertCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrelationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertCorrelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertEscalationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AlertType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MinSeverity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FirstEscalationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstEscalationRecipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SecondEscalationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    SecondEscalationRecipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FinalEscalationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    FinalEscalationRecipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EscalateViaEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    EscalateViaTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEscalationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertEscalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlertId = table.Column<int>(type: "INTEGER", nullable: false),
                    EscalationRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    EscalationLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Recipients = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MinutesSinceAlert = table.Column<int>(type: "INTEGER", nullable: false),
                    SentViaEmail = table.Column<bool>(type: "INTEGER", nullable: false),
                    SentViaTeams = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEscalations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CorrelationId",
                table: "Alerts",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Status",
                table: "Alerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertCorrelations_CorrelationId",
                table: "AlertCorrelations",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertCorrelations_FirstDetectedAt",
                table: "AlertCorrelations",
                column: "FirstDetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertCorrelations_Status",
                table: "AlertCorrelations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalationRules_AlertType",
                table: "AlertEscalationRules",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalationRules_Enabled",
                table: "AlertEscalationRules",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_AlertId",
                table: "AlertEscalations",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_EscalatedAt",
                table: "AlertEscalations",
                column: "EscalatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_EscalationRuleId",
                table: "AlertEscalations",
                column: "EscalationRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertCorrelations");

            migrationBuilder.DropTable(
                name: "AlertEscalationRules");

            migrationBuilder.DropTable(
                name: "AlertEscalations");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_CorrelationId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_Status",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "AcknowledgedBy",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Alerts");
        }
    }
}
