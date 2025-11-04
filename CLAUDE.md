# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**AXMonitoringBU** is an enterprise monitoring and analytics platform for Dynamics 365 AX (Finance & Operations). Recently migrated from Python/Streamlit to a modern .NET 9.0 stack with Blazor Server frontend and ASP.NET Core Web API backend.

## Technology Stack

- **Frontend**: Blazor Server (.NET 9.0) with Blazorise UI components
- **Backend**: ASP.NET Core Web API (.NET 9.0)
- **Database**: SQL Server 2016+ with Entity Framework Core (code-first)
- **Real-time**: SignalR for push notifications
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog (file-based)
- **Testing**: xUnit, Moq, FluentAssertions

## Architecture

### 3-Tier Architecture with Real-Time Updates

**1. Presentation Layer** (`AXMonitoringBU.Blazor/`)
- 11 routable Blazor pages: Overview, BatchJobs, Sessions, Blocking, SqlHealth, Alerts, Admin, TrendAnalysis, BusinessIntelligence, MLPredictions, AIAssistant
- SignalR client for real-time updates
- Bootstrap 5 UI via Blazorise components
- Chart.js for data visualization

**2. Business Logic & API Layer** (`AXMonitoringBU.Api/`)
- 12 REST API controllers: Metrics, Alerts, BatchJobs, Sessions, Database, Dashboards, Predictions, Reports, Remediation, Integrations, Notifications, Auth
- 12 corresponding service classes containing business logic
- SignalR `MonitoringHub` for real-time broadcasting
- Background `MonitoringUpdateService` for periodic data updates
- Entity Framework Core for data access

**3. Data Layer**
- `AXDbContext` managing all entities
- Core entities: `BatchJob`, `Session`, `BlockingChain`, `Alert`, `SqlHealth`, `RemediationRuleEntity`, `RemediationExecutionEntity`
- Code-first migrations for schema management

### Key Data Flow

1. API controllers receive HTTP requests
2. Controllers delegate to service layer for business logic
3. Services interact with `AXDbContext` for data persistence
4. Background services push updates via SignalR `MonitoringHub`
5. Blazor pages receive SignalR notifications and update UI reactively

## Development Commands

### Build & Restore
```bash
# Restore all dependencies
dotnet restore AXMonitoringBU.sln

# Build entire solution
dotnet build AXMonitoringBU.sln -c Release
```

### Run Development Servers
```bash
# Terminal 1: Run API (https://localhost:7001)
dotnet watch run --project AXMonitoringBU.Api/

# Terminal 2: Run Blazor frontend (https://localhost:7000)
dotnet watch run --project AXMonitoringBU.Blazor/
```

### Testing
```bash
# Run all tests with coverage
dotnet test AXMonitoringBU.sln --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test AXMonitoringBU.Api.Tests/
dotnet test AXMonitoringBU.Blazor.Tests/

# Run tests in watch mode
dotnet watch test --project AXMonitoringBU.Api.Tests/
```

### Database Migrations
```bash
# Apply pending migrations
dotnet ef database update --project AXMonitoringBU.Api

# Create new migration
dotnet ef migrations add MigrationName --project AXMonitoringBU.Api

# View migration SQL
dotnet ef migrations script --project AXMonitoringBU.Api
```

### Publishing
```bash
# Publish API
dotnet publish AXMonitoringBU.Api/ -c Release -o ./publish/api

# Publish Blazor frontend
dotnet publish AXMonitoringBU.Blazor/ -c Release -o ./publish/blazor
```

## Configuration

### Environment Variables

Both projects use `appsettings.json` with environment variable placeholders in the format `${VAR_NAME}`. See `env.example` for all required variables.

**API Configuration** (`AXMonitoringBU.Api/appsettings.json`):
- `ConnectionStrings:DefaultConnection` - SQL Server connection
- `MonitoringSettings` - Thresholds and intervals
- `EmailSettings` - SMTP configuration
- `TeamsSettings` - Webhook URLs
- `JwtSettings` - Authentication secrets
- `IntegrationSettings` - ServiceNow/Jira/Azure DevOps credentials

**Blazor Configuration** (`AXMonitoringBU.Blazor/appsettings.json`):
- `ApiSettings:BaseUrl` - API endpoint (default: https://localhost:7001)
- `App:Environment` - DEV/TST/PROD environment indicator

### Development Setup

1. Clone repository and restore packages
2. Copy `env.example` to `.env` and configure values
3. Update `appsettings.Development.json` with local database connection
4. Run migrations: `dotnet ef database update --project AXMonitoringBU.Api`
5. Start API and Blazor servers in separate terminals
6. Access Swagger UI at https://localhost:7001/swagger
7. Access application at https://localhost:7000

## Project Structure

### Backend (`AXMonitoringBU.Api/`)
- `Controllers/` - REST API endpoints (12 controllers)
- `Services/` - Business logic layer (12 services)
- `Data/AXDbContext.cs` - EF Core database context
- `Models/` - Domain entities
- `Hubs/MonitoringHub.cs` - SignalR hub for real-time updates
- `BackgroundServices/MonitoringUpdateService.cs` - Periodic data refresh
- `Program.cs` - Service registration and middleware configuration

### Frontend (`AXMonitoringBU.Blazor/`)
- `Components/Pages/` - Routable pages (11 pages)
- `Components/Layout/` - MainLayout and NavMenu
- `Services/` - Client-side API wrappers (8 services)
- `wwwroot/` - Static assets (CSS, JS, images)
- `Program.cs` - Service registration and SignalR client setup

### Testing
- `AXMonitoringBU.Api.Tests/` - xUnit tests for API
- `AXMonitoringBU.Blazor.Tests/` - xUnit tests for Blazor components

## Adding New Features

### Add a New Entity and API Endpoint

1. **Create Entity**: Add model class in `AXMonitoringBU.Api/Models/`
2. **Update DbContext**: Add `DbSet<YourEntity>` to `Data/AXDbContext.cs`
3. **Create Migration**: Run `dotnet ef migrations add AddYourEntity --project AXMonitoringBU.Api`
4. **Create Service**: Add interface and implementation in `Services/`
5. **Create Controller**: Add controller in `Controllers/`
6. **Register Service**: Add to DI container in `Program.cs`
7. **Apply Migration**: Run `dotnet ef database update --project AXMonitoringBU.Api`

### Add a New Blazor Page

1. **Create Page**: Add `.razor` file in `Components/Pages/`
2. **Add Route**: Use `@page "/your-route"` directive
3. **Create Service**: Add client-side service in `Services/` (if needed)
4. **Register Service**: Add to DI in `Program.cs` (if needed)
5. **Update Navigation**: Add link in `Components/Layout/NavMenu.razor`

### Add Real-Time Updates

1. **Server**: Add method to `Hubs/MonitoringHub.cs`
2. **Background**: Call hub method from `BackgroundServices/MonitoringUpdateService.cs`
3. **Client**: Subscribe in Blazor page using `@implements IAsyncDisposable` and `HubConnection`

## Key Integration Points

### SignalR Real-Time Updates
- **Hub**: `AXMonitoringBU.Api/Hubs/MonitoringHub.cs`
- **Endpoint**: `/monitoringHub` (configured in API's `Program.cs`)
- **Client Methods**: `ReceiveMetricsUpdate`, `ReceiveAlertUpdate`, `ReceiveBatchJobUpdate`, etc.

### Email Notifications
- **Service**: `NotificationService` using MailKit
- **Configuration**: `EmailSettings` in appsettings.json
- **Templates**: Formatted with severity-based styling

### External Ticketing Systems
- **Service**: `IntegrationService`
- **Supported**: ServiceNow, Jira, Azure DevOps
- **Webhooks**: Teams notifications for alerts

## Debugging

### Logs
- **Location**: `AXMonitoringBU.Api/logs/axmonitoring-YYYYMMDD.txt`
- **Format**: JSON-structured via Serilog
- **Levels**: Debug, Information, Warning, Error, Fatal

### API Debugging
- **Swagger UI**: https://localhost:7001/swagger
- **Endpoints**: Test all API endpoints interactively
- **Authentication**: Use `/api/auth/login` to get JWT token

### Common Issues

**Port Conflicts**: Change ports via `ASPNETCORE_URLS` environment variable or in `Properties/launchSettings.json`

**Database Connection**: Verify connection string and ensure SQL Server is running

**SignalR Connection**: Check CORS configuration in API's `Program.cs` and verify hub URL in Blazor's service registration

**Migration Errors**: Use `--verbose` flag for detailed output: `dotnet ef database update --verbose --project AXMonitoringBU.Api`

## CI/CD

GitHub Actions workflow in `.github/workflows/ci.yml`:
- **Triggers**: Push/PR to main or develop branches
- **Jobs**: build-api, build-blazor, security-scan
- **Coverage**: Uploaded to Codecov

## Documentation

- `DEPLOYMENT.md` - Deployment procedures and environment setup
- `MIGRATION_COMPLETE.md` - Python to .NET migration details
- `IMPLEMENTATION_STATUS.md` - Feature checklist and status
- `docs/` - Runbooks and SQL scripts

## Notes

- Uses JWT authentication with Bearer tokens
- Real-time updates via SignalR WebSocket connections
- Background service runs every 30 seconds (configurable)
- EF Core uses code-first migrations
- Bootstrap 5 styling via Blazorise component library
- Supports multi-environment deployments (DEV/TST/PROD)
