using Microsoft.EntityFrameworkCore;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Models;
using System.Collections.Generic;
using System.Linq;

namespace AXMonitoringBU.Api.Services;

public interface IScheduledReportService
{
    Task<List<ScheduledReport>> GetScheduledReportsAsync();
    Task<ScheduledReport> CreateScheduledReportAsync(ScheduledReport report);
    Task<bool> UpdateScheduledReportAsync(int id, ScheduledReport report);
    Task<bool> DeleteScheduledReportAsync(int id);
    Task<bool> ExecuteScheduledReportAsync(int id);
    Task ExecuteDueReportsAsync();
}

public class ScheduledReportService : IScheduledReportService
{
    private readonly AXDbContext _context;
    private readonly ILogger<ScheduledReportService> _logger;
    private readonly IPdfReportService _pdfReportService;
    private readonly IEmailAlertService? _emailService;
    private readonly IConfiguration _configuration;

    public ScheduledReportService(
        AXDbContext context,
        ILogger<ScheduledReportService> logger,
        IPdfReportService pdfReportService,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _pdfReportService = pdfReportService;
        _emailService = serviceProvider.GetService<IEmailAlertService>();
        _configuration = configuration;
    }

    public async Task<List<ScheduledReport>> GetScheduledReportsAsync()
    {
        try
        {
            return await _context.Set<ScheduledReport>()
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scheduled reports");
            throw;
        }
    }

    public async Task<ScheduledReport> CreateScheduledReportAsync(ScheduledReport report)
    {
        try
        {
            report.CreatedAt = DateTime.UtcNow;
            report.NextRun = CalculateNextRun(report);
            
            _context.Set<ScheduledReport>().Add(report);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created scheduled report: {Name}", report.Name);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled report");
            throw;
        }
    }

    public async Task<bool> UpdateScheduledReportAsync(int id, ScheduledReport report)
    {
        try
        {
            var existing = await _context.Set<ScheduledReport>().FindAsync(id);
            if (existing == null)
                return false;

            existing.Name = report.Name;
            existing.ReportType = report.ReportType;
            existing.Schedule = report.Schedule;
            existing.CronExpression = report.CronExpression;
            existing.Recipients = report.Recipients;
            existing.Enabled = report.Enabled;
            existing.NextRun = CalculateNextRun(report);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated scheduled report: {Name}", report.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled report");
            throw;
        }
    }

    public async Task<bool> DeleteScheduledReportAsync(int id)
    {
        try
        {
            var report = await _context.Set<ScheduledReport>().FindAsync(id);
            if (report == null)
                return false;

            _context.Set<ScheduledReport>().Remove(report);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted scheduled report: {Name}", report.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting scheduled report");
            throw;
        }
    }

    public async Task<bool> ExecuteScheduledReportAsync(int id)
    {
        try
        {
            var report = await _context.Set<ScheduledReport>().FindAsync(id);
            if (report == null || !report.Enabled)
                return false;

            _logger.LogInformation("Executing scheduled report: {Name}", report.Name);

            // Generate PDF report
            var reportData = await BuildReportDataAsync(CancellationToken.None);

            byte[] pdfBytes = report.ReportType.ToLowerInvariant() switch
            {
                "detailed" => await _pdfReportService.GenerateDetailedReportAsync(reportData, report.Schedule, CancellationToken.None),
                _ => await _pdfReportService.GenerateExecutiveSummaryAsync(reportData, report.Schedule, CancellationToken.None)
            };

            // Send email with PDF attachment
            if (_emailService != null && !string.IsNullOrEmpty(report.Recipients))
            {
                var recipients = report.Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                
                var subject = $"AX Monitoring Report: {report.Name} - {DateTime.UtcNow:yyyy-MM-dd}";
                var htmlBody = $@"
                    <html>
                    <body>
                        <h2>AX Monitoring Report</h2>
                        <p>Dear Recipient,</p>
                        <p>Please find attached the scheduled report <strong>{report.Name}</strong> ({report.ReportType}).</p>
                        <p>Report generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                        <p>Schedule: {report.Schedule}</p>
                        <p>Best regards,<br/>AX Monitoring System</p>
                    </body>
                    </html>";
                
                var textBody = $"AX Monitoring Report\n\nPlease find attached the scheduled report {report.Name} ({report.ReportType}).\n\nReport generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nSchedule: {report.Schedule}";
                
                var attachmentFileName = $"AX_Report_{report.Name}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                
                var emailSent = await _emailService.SendEmailWithAttachmentAsync(
                    recipients,
                    subject,
                    htmlBody,
                    textBody,
                    pdfBytes,
                    attachmentFileName,
                    CancellationToken.None);
                
                if (emailSent)
                {
                    _logger.LogInformation("Report {Name} sent successfully to {Recipients}", 
                        report.Name, string.Join(", ", recipients));
                }
                else
                {
                    _logger.LogWarning("Failed to send report {Name} to {Recipients}", 
                        report.Name, string.Join(", ", recipients));
                }
            }

            // Update last run and next run
            report.LastRun = DateTime.UtcNow;
            report.NextRun = CalculateNextRun(report);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled report {Id}", id);
            return false;
        }
    }

    public async Task ExecuteDueReportsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var dueReports = await _context.Set<ScheduledReport>()
                .Where(r => r.Enabled && 
                           r.NextRun.HasValue && 
                           r.NextRun <= now)
                .ToListAsync();

            foreach (var report in dueReports)
            {
                await ExecuteScheduledReportAsync(report.Id);
            }

            _logger.LogInformation("Executed {Count} due scheduled reports", dueReports.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing due reports");
        }
    }

    private async Task<ReportData> BuildReportDataAsync(CancellationToken cancellationToken)
    {
        var environment = _configuration["App:Environment"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "PROD";

        var now = DateTime.UtcNow;
        var since24Hours = now.AddHours(-24);

        var backlogCount = await _context.BatchJobs
            .CountAsync(b => b.Status == "Waiting" || b.Status == "Running", cancellationToken);

        var totalRecentJobs = await _context.BatchJobs
            .CountAsync(b => b.CreatedAt >= since24Hours, cancellationToken);

        var recentErrors = await _context.BatchJobs
            .CountAsync(b => b.CreatedAt >= since24Hours && b.Status == "Error", cancellationToken);

        var errorRate = totalRecentJobs > 0
            ? Math.Round((double)recentErrors / totalRecentJobs * 100, 2)
            : 0;

        var activeSessionsCount = await _context.Sessions
            .CountAsync(s => s.Status == "Active", cancellationToken);

        var blockingChainsCount = await _context.BlockingChains
            .CountAsync(b => b.ResolvedAt == null, cancellationToken);

        var latestSqlHealth = await _context.SqlHealthRecords
            .OrderByDescending(h => h.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var recentBatchJobs = await _context.BatchJobs
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.StartTime)
            .Take(10)
            .Select(b => new BatchJobDto
            {
                BatchJobId = b.BatchJobId,
                Name = b.Name,
                Status = b.Status,
                Progress = b.Progress
            })
            .ToListAsync(cancellationToken);

        var recentSessions = await _context.Sessions
            .OrderByDescending(s => s.LastActivity ?? s.LoginTime)
            .Take(10)
            .Select(s => new SessionDto
            {
                SessionId = s.SessionId,
                UserId = s.UserId,
                Status = s.Status
            })
            .ToListAsync(cancellationToken);

        var recentAlerts = await _context.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new AlertDto
            {
                Type = a.Type,
                Severity = a.Severity,
                Message = a.Message,
                Status = a.Status,
                Timestamp = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recommendations = GenerateRecommendations(errorRate, latestSqlHealth, recentAlerts);

        return new ReportData
        {
            Environment = environment,
            Kpis = new KpiData
            {
                batch_backlog = backlogCount,
                error_rate = errorRate,
                active_sessions = activeSessionsCount,
                blocking_chains = blockingChainsCount
            },
            SqlHealth = latestSqlHealth == null
                ? null
                : new SqlHealthData
                {
                    cpu_usage = latestSqlHealth.CpuUsage,
                    memory_usage = latestSqlHealth.MemoryUsage,
                    io_wait = latestSqlHealth.IoWait,
                    tempdb_usage = latestSqlHealth.TempDbUsage,
                    active_connections = latestSqlHealth.ActiveConnections,
                    longest_running_query = latestSqlHealth.LongestRunningQueryMinutes
                },
            BatchJobs = recentBatchJobs,
            Sessions = recentSessions,
            Alerts = recentAlerts,
            Recommendations = recommendations
        };
    }

    private List<string> GenerateRecommendations(double errorRate, SqlHealth? sqlHealth, IReadOnlyCollection<AlertDto> alerts)
    {
        var recommendations = new List<string>();

        if (errorRate > 5)
        {
            recommendations.Add("Investigate elevated batch job error rate (>5%) in the last 24 hours.");
        }

        if (sqlHealth != null)
        {
            if (sqlHealth.CpuUsage > 80)
            {
                recommendations.Add("SQL Server CPU usage is above 80%. Review long-running queries and index maintenance.");
            }

            if (sqlHealth.TempDbUsage > 70)
            {
                recommendations.Add("TempDB usage is high. Consider expanding TempDB files or reviewing TempDB-heavy workloads.");
            }
        }

        if (alerts.Any(a => a.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)))
        {
            recommendations.Add("Address critical alerts immediately to avoid SLA breaches.");
        }

        if (!recommendations.Any())
        {
            recommendations.Add("System operating within defined thresholds. Continue monitoring.");
        }

        return recommendations;
    }

    private DateTime? CalculateNextRun(ScheduledReport report)
    {
        var now = DateTime.UtcNow;
        
        return report.Schedule.ToLower() switch
        {
            "daily" => now.AddDays(1).Date.AddHours(8), // Next day at 8 AM
            "weekly" => now.AddDays(7).Date.AddHours(8), // Next week at 8 AM
            "monthly" => now.AddMonths(1).Date.AddDays(1).AddHours(8), // First day of next month at 8 AM
            _ => now.AddDays(1) // Default: daily
        };
    }
}

