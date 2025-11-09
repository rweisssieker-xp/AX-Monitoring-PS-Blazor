using Microsoft.AspNetCore.Mvc;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.Models;

namespace AXMonitoringBU.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v1/export-templates")]
public class ExportTemplatesController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ExportTemplatesController> _logger;

    public ExportTemplatesController(
        IExportService exportService,
        ILogger<ExportTemplatesController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetExportTemplates([FromQuery] string? entityType = null)
    {
        try
        {
            var templates = await _exportService.GetExportTemplatesAsync();
            
            if (!string.IsNullOrEmpty(entityType))
            {
                templates = templates.Where(t => t.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(new
            {
                export_templates = templates,
                count = templates.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export templates");
            return StatusCode(500, new { error = "Failed to retrieve export templates" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateExportTemplate([FromBody] CreateExportTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.EntityType))
            {
                return BadRequest(new { error = "Name and EntityType are required" });
            }

            var template = new ExportTemplate
            {
                Name = request.Name,
                EntityType = request.EntityType,
                Format = request.Format ?? "CSV",
                Fields = request.Fields ?? new List<string>(),
                IsDefault = request.IsDefault ?? false
            };

            var created = await _exportService.CreateExportTemplateAsync(template);
            return Ok(new
            {
                export_template = created,
                message = "Export template created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating export template");
            return StatusCode(500, new { error = "Failed to create export template" });
        }
    }
}

public class CreateExportTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Format { get; set; }
    public List<string>? Fields { get; set; }
    public bool? IsDefault { get; set; }
}

