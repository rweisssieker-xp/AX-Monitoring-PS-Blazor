using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Services;
using AXMonitoringBU.Api.BackgroundServices;
using AXMonitoringBU.Api.Middleware;
using AXMonitoringBU.Api.Swagger;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to prevent chunked encoding issues
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MinResponseDataRate = null;
    options.Limits.MaxResponseBufferSize = null;
    options.AllowSynchronousIO = true;

    // Disable HTTP/2 to avoid chunked encoding issues with Blazor
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
    });
});

// Configure Serilog with structured JSON logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AXMonitoringBU")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .WriteTo.File(
        new Serilog.Formatting.Json.JsonFormatter(),
        "logs/axmonitoring-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.File(
        "logs/axmonitoring-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Prevent JSON streaming to avoid chunked encoding issues
        options.JsonSerializerOptions.DefaultBufferSize = 1024 * 1024; // 1MB buffer
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddEndpointsApiExplorer();

// Add Response Compression to prevent chunked encoding issues
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = new[] { "application/json", "text/plain", "text/html" };
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new HeaderApiVersionReader("x-api-version"),
        new QueryStringApiVersionReader("api-version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// Add HttpContextAccessor for UserService
builder.Services.AddHttpContextAccessor();

// Configure Entity Framework for Monitoring Database (local SQLite for history)
// The AX Database connection is handled separately by AXDatabaseService
var monitoringDbConnectionString = builder.Configuration["MonitoringDatabase:ConnectionString"] 
    ?? "Data Source=axmonitoring.db";
var monitoringDbProvider = builder.Configuration["MonitoringDatabase:Provider"] ?? "Sqlite";

builder.Services.AddDbContext<AXDbContext>(options =>
{
    if (monitoringDbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(monitoringDbConnectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(60);
        });
    }
    else
    {
        options.UseSqlServer(monitoringDbConnectionString, sqlServerOptions =>
        {
            sqlServerOptions.CommandTimeout(60);
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
    
    // Query optimization
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Configure SignalR
builder.Services.AddSignalR();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Security:JwtSecret"] ?? "your-secret-key-change-in-production";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7000", "http://localhost:5000", "http://localhost:5108",
                "https://127.0.0.1:7000", "http://127.0.0.1:5000", "http://127.0.0.1:5108")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAXDatabaseService, AXDatabaseService>();
builder.Services.AddScoped<IKpiDataService, KpiDataService>();
builder.Services.AddScoped<IBatchJobService, BatchJobService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IBlockingService, BlockingService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IEmailAlertService, EmailAlertService>();
builder.Services.AddScoped<ITeamsNotificationService, TeamsNotificationService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddScoped<IBusinessKpiService, BusinessKpiService>();
builder.Services.AddScoped<IRemediationService, RemediationService>();
builder.Services.AddScoped<ITicketingService, TicketingService>();
builder.Services.AddScoped<IBaselineService, BaselineService>();
builder.Services.AddScoped<IMaintenanceWindowService, MaintenanceWindowService>();
builder.Services.AddScoped<IDeadlockService, DeadlockService>();
builder.Services.AddScoped<IDeadlockCaptureService, DeadlockCaptureService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IBatchJobHistoryAnalysisService, BatchJobHistoryAnalysisService>();
builder.Services.AddScoped<IWaitStatsService, WaitStatsService>();
builder.Services.AddScoped<IPerformanceBudgetService, PerformanceBudgetService>();
builder.Services.AddScoped<IArchivingService, ArchivingService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IScheduledReportService, ScheduledReportService>();
builder.Services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();
builder.Services.AddScoped<ISystemLoadAnalyticsService, SystemLoadAnalyticsService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();
builder.Services.AddScoped<IErrorAnalyticsService, ErrorAnalyticsService>();
builder.Services.AddScoped<IAlertEscalationService, AlertEscalationService>();
builder.Services.AddScoped<IAlertCorrelationService, AlertCorrelationService>();
builder.Services.AddScoped<ISharedDashboardService, SharedDashboardService>();
builder.Services.AddScoped<ICostTrackingService, CostTrackingService>();

// Add Memory Cache for caching service
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();

// Configure OpenAI Service
var analysisEnabled = builder.Configuration["OpenAI:AnalysisEnabled"] != "false";
if (analysisEnabled)
{
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<IOpenAIService>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient();
        var configuration = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<OpenAIService>>();
        return new OpenAIService(httpClient, configuration, logger);
    });
}
else
{
    // Dummy service if disabled
    builder.Services.AddSingleton<IOpenAIService, DummyOpenAIService>();
}

// Background Service for analysis (must be singleton)
builder.Services.AddSingleton<IBackgroundAnalysisService>(sp =>
{
    var openAIService = sp.GetRequiredService<IOpenAIService>();
    var logger = sp.GetRequiredService<ILogger<BackgroundAnalysisService>>();
    return new BackgroundAnalysisService(openAIService, logger);
});

// Register Background Service for SignalR updates
builder.Services.AddHostedService<MonitoringUpdateService>();
builder.Services.AddHostedService<DeadlockMonitoringService>();
builder.Services.AddHostedService<BaselineRecalculationService>();
builder.Services.AddHostedService<ArchivingBackgroundService>();
builder.Services.AddHostedService<ScheduledReportBackgroundService>();
builder.Services.AddHostedService<AlertEscalationBackgroundService>();
builder.Services.AddHostedService<AlertCorrelationBackgroundService>();
builder.Services.AddHostedService<CostBudgetBackgroundService>();

// Add HttpClient for Teams notifications
builder.Services.AddHttpClient();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<EmailHealthCheck>("email", tags: new[] { "ready" })
    .AddCheck<TeamsHealthCheck>("teams")
    .AddCheck<TicketingHealthCheck>("ticketing");

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AXDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // Use Migrate() for production, EnsureCreated() only for development
        if (app.Environment.IsDevelopment())
        {
            // In development, use EnsureCreated for faster startup
            // This will create the database if it doesn't exist but won't apply migrations
            context.Database.EnsureCreated();
            logger.LogInformation("Database ensured (Development mode)");
        }
        else
        {
            // In production, always use Migrate() to apply migrations properly
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied (Production mode)");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring database is created/migrated");
        throw; // Re-throw in production to prevent startup with broken database
    }
}

var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"AX Monitoring BU API {description.ApiVersion}");
        }
    });
}

app.UseSerilogRequestLogging();

// Add custom middleware
app.UseMiddleware<ForceContentLengthMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<PerformanceMonitoringMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<AXMonitoringBU.Api.Hubs.MonitoringHub>("/monitoringHub");

// Health check endpoints (also available via HealthController)
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Legacy health endpoint for backward compatibility
app.MapGet("/health/simple", () => new
{
    Status = "healthy",
    Timestamp = DateTime.UtcNow,
    Version = builder.Configuration["App:Version"] ?? "1.0.0"
});

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
