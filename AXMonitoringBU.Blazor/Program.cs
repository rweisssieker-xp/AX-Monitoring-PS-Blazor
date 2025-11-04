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
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add SignalR client - Changed to lazy initialization to avoid startup errors
builder.Services.AddSingleton<HubConnection>(sp =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7001";
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
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IBlockingService, BlockingService>();
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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
