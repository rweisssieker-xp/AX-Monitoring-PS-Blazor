using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Services;

public interface IPdfReportService
{
    Task<byte[]> GenerateExecutiveSummaryAsync(ReportData data, string period = "monthly", CancellationToken cancellationToken = default);
    Task<byte[]> GenerateDetailedReportAsync(ReportData data, string period = "monthly", CancellationToken cancellationToken = default);
}

public class PdfReportService : IPdfReportService
{
    private readonly ILogger<PdfReportService> _logger;

    public PdfReportService(ILogger<PdfReportService> logger)
    {
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateExecutiveSummaryAsync(ReportData data, string period = "monthly", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("AX 2012 R3 Monitor - Executive Summary")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2).AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Report Metadata
                            column.Item().Border(1).Padding(10).Column(meta =>
                            {
                                meta.Item().Text("Report Period:").SemiBold();
                                meta.Item().Text(period);
                                meta.Item().PaddingTop(5).Text("Generated:").SemiBold();
                                meta.Item().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                meta.Item().PaddingTop(5).Text("Environment:").SemiBold();
                                meta.Item().Text(data.Environment ?? "PROD");
                            });

                            // Executive Summary Section
                            column.Item().Text("Executive Summary").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                            
                            var summary = GenerateSummaryText(data);
                            column.Item().Text(summary).FontSize(11);

                            // Key Metrics
                            column.Item().Text("Key Performance Indicators").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                            
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Metric").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Current").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Target").SemiBold();
                                    header.Cell().Element(CellStyle).Text("Status").SemiBold();
                                });

                                if (data.Kpis != null)
                                {
                                    AddMetricRow(table, "Batch Backlog", data.Kpis.batch_backlog.ToString(), "0", data.Kpis.batch_backlog == 0 ? "Good" : "Warning");
                                    AddMetricRow(table, "Error Rate", $"{data.Kpis.error_rate:F1}%", "<5%", data.Kpis.error_rate < 5 ? "Good" : "Warning");
                                    AddMetricRow(table, "Active Sessions", data.Kpis.active_sessions.ToString(), "N/A", "Good");
                                    AddMetricRow(table, "Blocking Chains", data.Kpis.blocking_chains.ToString(), "0", data.Kpis.blocking_chains == 0 ? "Good" : "Warning");
                                }
                            });

                            // Recommendations
                            if (data.Recommendations != null && data.Recommendations.Any())
                            {
                                column.Item().Text("Recommendations").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                                foreach (var recommendation in data.Recommendations)
                                {
                                    column.Item().Text($"• {recommendation}").FontSize(11);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf();
        }, cancellationToken);
    }

    public async Task<byte[]> GenerateDetailedReportAsync(ReportData data, string period = "monthly", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("AX 2012 R3 Monitor - Detailed Report")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2).AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Table of Contents
                            column.Item().Text("Table of Contents").FontSize(18).SemiBold();
                            column.Item().Text("1. Executive Summary");
                            column.Item().Text("2. System Performance Metrics");
                            column.Item().Text("3. Batch Job Analysis");
                            column.Item().Text("4. Session Management");
                            column.Item().Text("5. Database Health");
                            column.Item().Text("6. Alert Analysis");
                            column.Item().Text("7. Recommendations");

                            // Executive Summary
                            column.Item().PageBreak();
                            column.Item().Text("1. Executive Summary").FontSize(16).SemiBold();
                            column.Item().Text(GenerateSummaryText(data));

                            // System Performance Metrics
                            column.Item().PageBreak();
                            column.Item().Text("2. System Performance Metrics").FontSize(16).SemiBold();
                            
                            if (data.SqlHealth != null)
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Element(CellStyle).Text("CPU Usage").SemiBold();
                                    table.Cell().Element(CellStyle).Text($"{data.SqlHealth.cpu_usage:F1}%");

                                    table.Cell().Element(CellStyle).Text("Memory Usage").SemiBold();
                                    table.Cell().Element(CellStyle).Text($"{data.SqlHealth.memory_usage:F1}%");

                                    table.Cell().Element(CellStyle).Text("IO Wait").SemiBold();
                                    table.Cell().Element(CellStyle).Text($"{data.SqlHealth.io_wait:F1}%");

                                    table.Cell().Element(CellStyle).Text("TempDB Usage").SemiBold();
                                    table.Cell().Element(CellStyle).Text($"{data.SqlHealth.tempdb_usage:F1}%");

                                    table.Cell().Element(CellStyle).Text("Active Connections").SemiBold();
                                    table.Cell().Element(CellStyle).Text(data.SqlHealth.active_connections.ToString());

                                    table.Cell().Element(CellStyle).Text("Longest Running Query").SemiBold();
                                    table.Cell().Element(CellStyle).Text($"{data.SqlHealth.longest_running_query} minutes");
                                });
                            }

                            // Batch Job Analysis
                            column.Item().PageBreak();
                            column.Item().Text("3. Batch Job Analysis").FontSize(16).SemiBold();
                            
                            if (data.BatchJobs != null && data.BatchJobs.Any())
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Job ID").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Name").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Status").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Progress").SemiBold();
                                    });

                                    foreach (var job in data.BatchJobs.Take(20))
                                    {
                                        table.Cell().Element(CellStyle).Text(job.BatchJobId);
                                        table.Cell().Element(CellStyle).Text(job.Name);
                                        table.Cell().Element(CellStyle).Text(job.Status);
                                        table.Cell().Element(CellStyle).Text($"{job.Progress}%");
                                    }
                                });
                            }

                            // Alert Analysis
                            column.Item().PageBreak();
                            column.Item().Text("6. Alert Analysis").FontSize(16).SemiBold();
                            
                            if (data.Alerts != null && data.Alerts.Any())
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyle).Text("Type").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Severity").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Message").SemiBold();
                                        header.Cell().Element(CellStyle).Text("Timestamp").SemiBold();
                                    });

                                    foreach (var alert in data.Alerts.Take(20))
                                    {
                                        table.Cell().Element(CellStyle).Text(alert.Type);
                                        table.Cell().Element(CellStyle).Text(alert.Severity);
                                        table.Cell().Element(CellStyle).Text(alert.Message.Length > 50 ? alert.Message.Substring(0, 50) + "..." : alert.Message);
                                        table.Cell().Element(CellStyle).Text(alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                });
                            }

                            // Recommendations
                            column.Item().PageBreak();
                            column.Item().Text("7. Recommendations").FontSize(16).SemiBold();
                            
                            if (data.Recommendations != null && data.Recommendations.Any())
                            {
                                foreach (var recommendation in data.Recommendations)
                                {
                                    column.Item().Text($"• {recommendation}").FontSize(11);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf();
        }, cancellationToken);
    }

    private string GenerateSummaryText(ReportData data)
    {
        var text = $"This report provides a comprehensive overview of the AX 2012 R3 system performance and health metrics.\n\n";
        
        if (data.Kpis != null)
        {
            text += $"System Status: ";
            var issues = new List<string>();
            if (data.Kpis.error_rate > 5) issues.Add("Elevated error rate");
            if (data.Kpis.blocking_chains > 0) issues.Add("Active blocking chains");
            if (data.SqlHealth != null && data.SqlHealth.cpu_usage > 80) issues.Add("High CPU usage");
            
            if (issues.Any())
            {
                text += $"Needs Attention - {string.Join(", ", issues)}.\n";
            }
            else
            {
                text += "All systems operating within normal parameters.\n";
            }
        }

        text += "\nKey Highlights:\n";
        if (data.BatchJobs != null)
        {
            var runningJobs = data.BatchJobs.Count(j => j.Status == "Running");
            var errorJobs = data.BatchJobs.Count(j => j.Status == "Error");
            text += $"- {runningJobs} batch jobs currently running\n";
            if (errorJobs > 0)
            {
                text += $"- {errorJobs} batch jobs with errors\n";
            }
        }

        if (data.Alerts != null)
        {
            var criticalAlerts = data.Alerts.Count(a => a.Severity == "Critical");
            var activeAlerts = data.Alerts.Count(a => a.Status == "Active");
            text += $"- {activeAlerts} active alerts ({criticalAlerts} critical)\n";
        }

        return text;
    }

    private void AddMetricRow(QuestPDF.Fluent.TableDescriptor table, string metric, string current, string target, string status)
    {
        table.Cell().Element(CellStyle).Text(metric);
        table.Cell().Element(CellStyle).Text(current);
        table.Cell().Element(CellStyle).Text(target);
        table.Cell().Element(CellStyle).Text(status).FontColor(status == "Good" ? Colors.Green.Darken2 : Colors.Orange.Darken2);
    }

    private IContainer CellStyle(IContainer container)
    {
        return container
            .Border(1)
            .Padding(5)
            .BorderColor(Colors.Grey.Lighten2);
    }
}

public class ReportData
{
    public string? Environment { get; set; }
    public KpiData? Kpis { get; set; }
    public SqlHealthData? SqlHealth { get; set; }
    public List<BatchJobDto>? BatchJobs { get; set; }
    public List<SessionDto>? Sessions { get; set; }
    public List<AlertDto>? Alerts { get; set; }
    public List<string>? Recommendations { get; set; }
}

public class KpiData
{
    public int batch_backlog { get; set; }
    public double error_rate { get; set; }
    public int active_sessions { get; set; }
    public int blocking_chains { get; set; }
}

public class SqlHealthData
{
    public double cpu_usage { get; set; }
    public double memory_usage { get; set; }
    public double io_wait { get; set; }
    public double tempdb_usage { get; set; }
    public int active_connections { get; set; }
    public int longest_running_query { get; set; }
}

public class BatchJobDto
{
    public string BatchJobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
}

public class SessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

