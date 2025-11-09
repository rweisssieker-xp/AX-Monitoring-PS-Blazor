using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using AXMonitoringBU.Api.Models;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AXMonitoringBU.Api.Services;

public interface IExportService
{
    Task<byte[]> ExportBatchJobsToCsvAsync(IEnumerable<BatchJob> batchJobs, string? template = null);
    Task<byte[]> ExportSessionsToCsvAsync(IEnumerable<Session> sessions, string? template = null);
    Task<byte[]> ExportAlertsToCsvAsync(IEnumerable<Alert> alerts, string? template = null);
    Task<byte[]> ExportBatchJobsToExcelAsync(IEnumerable<BatchJob> batchJobs, string? template = null);
    Task<byte[]> ExportSessionsToExcelAsync(IEnumerable<Session> sessions, string? template = null);
    Task<byte[]> ExportAlertsToExcelAsync(IEnumerable<Alert> alerts, string? template = null);
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName);
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName);
    Task<List<ExportTemplate>> GetExportTemplatesAsync();
    Task<ExportTemplate> CreateExportTemplateAsync(ExportTemplate template);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;
    private readonly AXDbContext _context;

    public ExportService(ILogger<ExportService> logger, AXDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<byte[]> ExportBatchJobsToCsvAsync(IEnumerable<BatchJob> batchJobs, string? template = null)
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

    public async Task<byte[]> ExportSessionsToCsvAsync(IEnumerable<Session> sessions, string? template = null)
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

    public async Task<byte[]> ExportAlertsToCsvAsync(IEnumerable<Alert> alerts, string? template = null)
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

    public async Task<byte[]> ExportBatchJobsToExcelAsync(IEnumerable<BatchJob> batchJobs, string? template = null)
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

    public async Task<byte[]> ExportSessionsToExcelAsync(IEnumerable<Session> sessions, string? template = null)
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

    public async Task<byte[]> ExportAlertsToExcelAsync(IEnumerable<Alert> alerts, string? template = null)
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

    public async Task<List<ExportTemplate>> GetExportTemplatesAsync()
    {
        try
        {
            var templates = await _context.ExportTemplates.ToListAsync();
            
            // If no templates exist, create default ones
            if (!templates.Any())
            {
                var defaultTemplates = new List<ExportTemplate>
                {
                    new ExportTemplate
                    {
                        Name = "Standard",
                        EntityType = "BatchJob",
                        Format = "CSV",
                        Fields = new List<string> { "BatchJobId", "Name", "Status", "StartTime", "EndTime", "Progress" },
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ExportTemplate
                    {
                        Name = "Detailed",
                        EntityType = "BatchJob",
                        Format = "Excel",
                        Fields = new List<string> { "BatchJobId", "Name", "Status", "StartTime", "EndTime", "EstimatedDuration", "Progress", "AosServer", "CreatedAt" },
                        IsDefault = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ExportTemplate
                    {
                        Name = "Standard",
                        EntityType = "Session",
                        Format = "CSV",
                        Fields = new List<string> { "SessionId", "UserId", "AosServer", "Status", "LoginTime", "LastActivity" },
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ExportTemplate
                    {
                        Name = "Standard",
                        EntityType = "Alert",
                        Format = "CSV",
                        Fields = new List<string> { "AlertId", "Type", "Severity", "Message", "Status", "Timestamp", "ResolvedAt" },
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.ExportTemplates.AddRange(defaultTemplates);
                await _context.SaveChangesAsync();
                templates = defaultTemplates;
            }

            return templates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving export templates");
            // Return empty list on error
            return new List<ExportTemplate>();
        }
    }

    public async Task<ExportTemplate> CreateExportTemplateAsync(ExportTemplate template)
    {
        try
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            
            _context.ExportTemplates.Add(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created export template: {Name} for {EntityType}", template.Name, template.EntityType);
            return template;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating export template: {Name}", template.Name);
            throw;
        }
    }

    /// <summary>
    /// Generic method to export any IEnumerable to CSV
    /// </summary>
    public async Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, string fileName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteRecords(data);
            await writer.FlushAsync();
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to CSV: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Generic method to export any IEnumerable to Excel
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName)
    {
        try
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            var dataList = data.ToList();
            if (dataList.Any())
            {
                // Get properties
                var properties = typeof(T).GetProperties();

                // Add headers
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                    worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                }

                // Add data
                for (int row = 0; row < dataList.Count; row++)
                {
                    var item = dataList[row];
                    for (int col = 0; col < properties.Length; col++)
                    {
                        var value = properties[col].GetValue(item);
                        worksheet.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
                    }
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return await Task.FromResult(stream.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data to Excel: {SheetName}", sheetName);
            throw;
        }
    }
}

