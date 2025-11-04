using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using AXMonitoringBU.Api.Data;
using AXMonitoringBU.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/axmonitoring-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
        options.UseSqlite(monitoringDbConnectionString);
    }
    else
    {
        options.UseSqlServer(monitoringDbConnectionString);
    }
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
        policy.WithOrigins("https://localhost:7000", "http://localhost:5000", "http://localhost:5108")
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
builder.Services.AddScoped<IExportService, ExportService>();
    builder.Services.AddScoped<IBatchJobHistoryAnalysisService, BatchJobHistoryAnalysisService>();

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

// Add HttpClient for Teams notifications
builder.Services.AddHttpClient();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<EmailHealthCheck>("email")
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
        context.Database.EnsureCreated();
        // Note: In production, use migrations instead:
        // context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while ensuring database is created");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<AXMonitoringBU.Api.Hubs.MonitoringHub>("/monitoringHub");

// Health check endpoints
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
