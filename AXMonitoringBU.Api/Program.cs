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

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? (builder.Configuration["Database:Server"] != null
        ? $"Server={builder.Configuration["Database:Server"]};Database={builder.Configuration["Database:Name"]};User Id={builder.Configuration["Database:User"]};Password={builder.Configuration["Database:Password"]};TrustServerCertificate=true;Connection Timeout={builder.Configuration["Database:ConnectionTimeout"] ?? "30"};Command Timeout={builder.Configuration["Database:CommandTimeout"] ?? "60"}"
        : "Server=localhost;Database=AXMonitoringBU;Trusted_Connection=true;TrustServerCertificate=true");

var dbProvider = builder.Configuration["Database:Provider"] ?? "SqlServer";

builder.Services.AddDbContext<AXDbContext>(options =>
{
    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
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

// Register Background Service for SignalR updates
builder.Services.AddHostedService<MonitoringUpdateService>();

// Add HttpClient for Teams notifications
builder.Services.AddHttpClient();

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

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "healthy",
    Timestamp = DateTime.UtcNow,
    Version = builder.Configuration["App:Version"] ?? "1.0.0"
});

app.Run();
