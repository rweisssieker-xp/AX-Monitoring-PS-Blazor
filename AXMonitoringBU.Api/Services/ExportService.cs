using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;

namespace AXMonitoringBU.Api.Services;

public interface IExportService
{
    Task<byte[]> ExportBatchJobsToCsvAsync(IEnumerable<BatchJob> batchJobs);
    Task<byte[]> ExportSessionsToCsvAsync(IEnumerable<Session> sessions);
    Task<byte[]> ExportAlertsToCsvAsync(IEnumerable<Alert> alerts);
    Task<byte[]> ExportBatchJobsToExcelAsync(IEnumerable<BatchJob> batchJobs);
    Task<byte[]> ExportSessionsToExcelAsync(IEnumerable<Session> sessions);
    Task<byte[]> ExportAlertsToExcelAsync(IEnumerable<Alert> alerts);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> ExportBatchJobsToCsvAsync(IEnumerable<BatchJob> batchJobs)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteRecords(batchJobs.Select(b => new
            {
                b.BatchJobId,
                b.Name,
                b.Status,
                StartTime = b.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                EndTime = b.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                b.EstimatedDuration,
                b.Progress,
                b.AosServer,
                CreatedAt = b.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = b.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
            }));

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting batch jobs to CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportSessionsToCsvAsync(IEnumerable<Session> sessions)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteRecords(sessions.Select(s => new
            {
                s.SessionId,
                s.UserId,
                s.AosServer,
                s.Status,
                LoginTime = s.LoginTime.ToString("yyyy-MM-dd HH:mm:ss"),
                LastActivity = s.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                s.Database,
                CreatedAt = s.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = s.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
            }));

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sessions to CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportAlertsToCsvAsync(IEnumerable<Alert> alerts)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteRecords(alerts.Select(a => new
            {
                a.AlertId,
                a.Type,
                a.Severity,
                a.Message,
                a.Status,
                Timestamp = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                ResolvedAt = a.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                a.CreatedBy,
                CreatedAt = a.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                UpdatedAt = a.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""
            }));

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts to CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportBatchJobsToExcelAsync(IEnumerable<BatchJob> batchJobs)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Batch Jobs");

            // Headers
            worksheet.Cell(1, 1).Value = "Batch Job ID";
            worksheet.Cell(1, 2).Value = "Name";
            worksheet.Cell(1, 3).Value = "Status";
            worksheet.Cell(1, 4).Value = "Start Time";
            worksheet.Cell(1, 5).Value = "End Time";
            worksheet.Cell(1, 6).Value = "Estimated Duration";
            worksheet.Cell(1, 7).Value = "Progress";
            worksheet.Cell(1, 8).Value = "AOS Server";
            worksheet.Cell(1, 9).Value = "Created At";
            worksheet.Cell(1, 10).Value = "Updated At";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data
            int row = 2;
            foreach (var job in batchJobs)
            {
                worksheet.Cell(row, 1).Value = job.BatchJobId;
                worksheet.Cell(row, 2).Value = job.Name;
                worksheet.Cell(row, 3).Value = job.Status;
                worksheet.Cell(row, 4).Value = job.StartTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(row, 5).Value = job.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(row, 6).Value = job.EstimatedDuration ?? 0;
                worksheet.Cell(row, 7).Value = job.Progress;
                worksheet.Cell(row, 8).Value = job.AosServer;
                worksheet.Cell(row, 9).Value = job.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 10).Value = job.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return await Task.FromResult(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting batch jobs to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportSessionsToExcelAsync(IEnumerable<Session> sessions)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sessions");

            // Headers
            worksheet.Cell(1, 1).Value = "Session ID";
            worksheet.Cell(1, 2).Value = "User ID";
            worksheet.Cell(1, 3).Value = "AOS Server";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Login Time";
            worksheet.Cell(1, 6).Value = "Last Activity";
            worksheet.Cell(1, 7).Value = "Database";
            worksheet.Cell(1, 8).Value = "Created At";
            worksheet.Cell(1, 9).Value = "Updated At";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data
            int row = 2;
            foreach (var session in sessions)
            {
                worksheet.Cell(row, 1).Value = session.SessionId;
                worksheet.Cell(row, 2).Value = session.UserId;
                worksheet.Cell(row, 3).Value = session.AosServer;
                worksheet.Cell(row, 4).Value = session.Status;
                worksheet.Cell(row, 5).Value = session.LoginTime.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 6).Value = session.LastActivity?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(row, 7).Value = session.Database;
                worksheet.Cell(row, 8).Value = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 9).Value = session.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return await Task.FromResult(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting sessions to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportAlertsToExcelAsync(IEnumerable<Alert> alerts)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Alerts");

            // Headers
            worksheet.Cell(1, 1).Value = "Alert ID";
            worksheet.Cell(1, 2).Value = "Type";
            worksheet.Cell(1, 3).Value = "Severity";
            worksheet.Cell(1, 4).Value = "Message";
            worksheet.Cell(1, 5).Value = "Status";
            worksheet.Cell(1, 6).Value = "Timestamp";
            worksheet.Cell(1, 7).Value = "Resolved At";
            worksheet.Cell(1, 8).Value = "Created By";
            worksheet.Cell(1, 9).Value = "Created At";
            worksheet.Cell(1, 10).Value = "Updated At";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data
            int row = 2;
            foreach (var alert in alerts)
            {
                worksheet.Cell(row, 1).Value = alert.AlertId;
                worksheet.Cell(row, 2).Value = alert.Type;
                worksheet.Cell(row, 3).Value = alert.Severity;
                worksheet.Cell(row, 4).Value = alert.Message;
                worksheet.Cell(row, 5).Value = alert.Status;
                worksheet.Cell(row, 6).Value = alert.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 7).Value = alert.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                worksheet.Cell(row, 8).Value = alert.CreatedBy ?? "";
                worksheet.Cell(row, 9).Value = alert.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 10).Value = alert.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            return await Task.FromResult(memoryStream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting alerts to Excel");
            throw;
        }
    }
}

