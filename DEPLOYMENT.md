# Deployment Documentation

## Prerequisites

- .NET 9.0 SDK
- SQL Server 2016+ (or compatible database)
- Windows Server / Linux Server with .NET Runtime

## Manual Deployment

### 1. Build the Solution

```bash
# Restore dependencies
dotnet restore AXMonitoringBU.sln

# Build Release version
dotnet build AXMonitoringBU.sln -c Release
```

### 2. Publish Applications

#### API Application
```bash
dotnet publish AXMonitoringBU.Api/AXMonitoringBU.Api.csproj -c Release -o ./publish/api
```

#### Blazor Application
```bash
dotnet publish AXMonitoringBU.Blazor/AXMonitoringBU.Blazor.csproj -c Release -o ./publish/blazor
```

### 3. Configure Environment Variables

Create `appsettings.Production.json` files or set environment variables:

#### API (`AXMonitoringBU.Api/appsettings.Production.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=AXMonitoringBU;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=true"
  },
  "Database": {
    "Server": "YOUR_SERVER",
    "Name": "AXMonitoringBU",
    "User": "YOUR_USER",
    "Password": "YOUR_PASSWORD"
  },
  "Security": {
    "JwtSecret": "YOUR_SECRET_KEY"
  }
}
```

#### Blazor (`AXMonitoringBU.Blazor/appsettings.Production.json`)
```json
{
  "App": {
    "Environment": "PROD"
  },
  "ApiSettings": {
    "BaseUrl": "https://your-api-domain.com"
  }
}
```

### 4. Database Migration

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Run migrations
cd AXMonitoringBU.Api
dotnet ef database update
```

### 5. Run Applications

#### API (Terminal 1)
```bash
cd publish/api
dotnet AXMonitoringBU.Api.dll
```

#### Blazor (Terminal 2)
```bash
cd publish/blazor
dotnet AXMonitoringBU.Blazor.dll
```

### 6. Windows Service / Systemd Service (Optional)

#### Windows Service
```bash
# Install as Windows Service using sc.exe or NSSM
sc create AXMonitoringAPI binPath="C:\path\to\publish\api\AXMonitoringBU.Api.exe"
sc start AXMonitoringAPI
```

#### Linux Systemd Service
```ini
[Unit]
Description=AX Monitoring API
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /path/to/publish/api/AXMonitoringBU.Api.dll
Restart=always

[Install]
WantedBy=multi-user.target
```

## CI/CD Pipeline

### GitHub Actions

The project includes a CI Pipeline (`.github/workflows/ci.yml`):
- Builds and tests on every push/PR
- Runs security scans
- Validates code quality

## Environment Variables

Required environment variables:

### API
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Database__Server` - SQL Server hostname
- `Database__Name` - Database name
- `Database__User` - Database user
- `Database__Password` - Database password
- `Security__JwtSecret` - JWT secret key
- `Alerts__Email__*` - Email configuration
- `Alerts__Teams__*` - Teams webhook URLs

### Blazor
- `ApiSettings__BaseUrl` - API base URL
- `App__Environment` - Environment name (DEV/TST/PROD)

See `.env.example` for complete list.

## Health Checks

### API Health Endpoint
```bash
curl http://localhost:7001/health
```

### Blazor Health
Access http://localhost:7000 - should display the application

## IIS Deployment (Windows)

### 1. Install ASP.NET Core Hosting Bundle
Download and install from: https://dotnet.microsoft.com/download

### 2. Create IIS Application Pools
- Create separate app pools for API and Blazor
- Set .NET CLR Version to "No Managed Code"
- Set Managed Pipeline Mode to "Integrated"

### 3. Configure Applications
- Point API site to `publish/api` folder
- Point Blazor site to `publish/blazor` folder
- Configure bindings (ports, SSL certificates)

## Troubleshooting

### Check Logs
- API logs: `logs/axmonitoring-*.txt`
- Application Event Log (Windows)
- Journalctl (Linux)

### Database Connection Issues
```bash
# Test connection string
dotnet ef database update --project AXMonitoringBU.Api --verbose
```

### Port Conflicts
- Default API port: 7001 (HTTPS), 5001 (HTTP)
- Default Blazor port: 7000 (HTTPS), 5000 (HTTP)
- Configure in `launchSettings.json` or `appsettings.json`

