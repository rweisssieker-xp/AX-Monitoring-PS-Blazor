using AXMonitoringBU.Blazor.Components;
using Microsoft.AspNetCore.SignalR.Client;
using AXMonitoringBU.Blazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HttpClient for API calls
builder.Services.AddHttpClient("ApiClient", client =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://127.0.0.1:5079";
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    // Force HTTP/1.1 to avoid chunked encoding issues
    client.DefaultRequestVersion = new Version(1, 1);
    client.DefaultVersionPolicy = System.Net.Http.HttpVersionPolicy.RequestVersionOrLower;
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new SocketsHttpHandler
    {
        // Use connection pooling and keep-alive
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        // Disable response buffering to handle chunked encoding better
        AutomaticDecompression = System.Net.DecompressionMethods.None,
        // Allow any certificate for localhost
        SslOptions = new System.Net.Security.SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
        }
    };
});

// Add SignalR client - Changed to lazy initialization to avoid startup errors
builder.Services.AddSingleton<HubConnection>(sp =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://127.0.0.1:5079";
    var hubConnection = new HubConnectionBuilder()
        .WithUrl($"{apiUrl}/monitoringHub", options =>
        {
            options.HttpMessageHandlerFactory = handler => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        })
        .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
        .Build();

    // Don't start connection immediately - let SignalRService handle it
    return hubConnection;
});

// Register frontend services
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IPredictionsService, PredictionsService>();
builder.Services.AddScoped<IBatchJobService, BatchJobService>();
builder.Services.AddScoped<IBatchJobHistoryService, BatchJobHistoryService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IBlockingService, BlockingService>();
builder.Services.AddScoped<IDeadlockService, DeadlockService>();
builder.Services.AddScoped<IPreferencesService, PreferencesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ISystemLoadAnalyticsService, SystemLoadAnalyticsService>();
builder.Services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();
builder.Services.AddScoped<IErrorAnalyticsService, ErrorAnalyticsService>();
// SignalR service must be Singleton to match HubConnection lifecycle
builder.Services.AddSingleton<ISignalRService, SignalRService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
