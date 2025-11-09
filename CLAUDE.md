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
- **Logging**: Serilog (file-based, dual format: text + JSON)
- **Testing**: xUnit, Moq, FluentAssertions
- **Containerization**: Docker with multi-stage builds, docker-compose for local development

**Development Environment**: Project developed primarily on Windows but supports cross-platform deployment (Windows/Linux/Docker).

## Architecture

### 3-Tier Architecture with Real-Time Updates

**1. Presentation Layer** (`AXMonitoringBU.Blazor/`)
- 14 routable Blazor pages: Overview, Home, BatchJobs, BatchJobHistory, Sessions, Blocking, SqlHealth, Alerts, Admin, TrendAnalysis, BusinessIntelligence, MLPredictions, AIAssistant, Error
- SignalR client for real-time updates
- Bootstrap 5 UI via Blazorise components
- Chart.js for data visualization

**2. Business Logic & API Layer** (`AXMonitoringBU.Api/`)
- 23 REST API controllers including: Metrics, MetricsExport, Alerts, BatchJobs, BatchJobHistory, Sessions, Database, Dashboards, Predictions, Reports, Remediation, Integrations, Notifications, Auth, Deadlocks, Webhooks, WaitStats, ScheduledReports, PerformanceBudget, Health, ExportTemplates, BulkOperations, Archiving
- 31 service classes containing business logic
- SignalR `MonitoringHub` for real-time broadcasting
- 5 background services: `MonitoringUpdateService`, `ArchivingBackgroundService`, `ScheduledReportBackgroundService`, `DeadlockMonitoringService`, `BaselineRecalculationService`
- 3 custom middleware: `CorrelationIdMiddleware`, `RateLimitingMiddleware`, `PerformanceMonitoringMiddleware`
- Entity Framework Core for data access
- API versioning support (v1, v2) via header (`x-api-version`) or query string (`api-version`)
- **GlobalUsings.cs** defines common namespace imports project-wide for cleaner code

**3. Data Layer**
- **Two-database architecture**:
  - **Monitoring Database** (SQLite by default, configurable to SQL Server): Stores alerts, metrics history, dashboards, and application data. Managed by `AXDbContext` with EF Core code-first migrations.
  - **AX Database** (SQL Server): Read-only connection to Dynamics AX database for querying batch jobs, sessions, and performance data. Accessed via `AXDatabaseService`.
- Core entities: `BatchJob`, `Session`, `BlockingChain`, `Alert`, `SqlHealth`, `RemediationRuleEntity`, `RemediationExecutionEntity`, `PerformanceBudget`, `ScheduledReport`
- Code-first migrations for Monitoring Database schema management

### Key Data Flow

1. API controllers receive HTTP requests
2. Controllers delegate to service layer for business logic
3. Services query **AX Database** (read-only via `AXDatabaseService`) and persist to **Monitoring Database** (via `AXDbContext`)
4. Background services collect metrics from AX Database and push updates via SignalR `MonitoringHub`
5. Blazor pages receive SignalR notifications and update UI reactively

**Important**: The dual-database pattern means `AXDatabaseService` directly queries AX SQL tables while `AXDbContext` manages the local monitoring data store.

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

# Run a single test by name
dotnet test --filter "FullyQualifiedName~KpiDataServiceTests.GetKpiData_ReturnsData"
dotnet test --filter "DisplayName~GetKpiData"

# Run all tests in a specific class
dotnet test --filter "FullyQualifiedName~KpiDataServiceTests"

# Run tests in watch mode
dotnet watch test --project AXMonitoringBU.Api.Tests/

# Run tests with detailed output
dotnet test --verbosity detailed
```

**Note**: CI pipeline runs tests with `continue-on-error: true` to allow builds to complete even if some tests fail. This is intentional during active development. Test coverage is ~95% complete for core services.

### Database Migrations
```bash
# Install EF Core tools globally (if not already installed)
dotnet tool install --global dotnet-ef

# Apply pending migrations
dotnet ef database update --project AXMonitoringBU.Api

# Create new migration
dotnet ef migrations add MigrationName --project AXMonitoringBU.Api

# View migration SQL
dotnet ef migrations script --project AXMonitoringBU.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project AXMonitoringBU.Api
```

**Note**: Migrations only apply to the Monitoring Database. The AX Database is read-only and schema changes are not supported.

### Publishing
```bash
# Publish API
dotnet publish AXMonitoringBU.Api/ -c Release -o ./publish/api

# Publish Blazor frontend
dotnet publish AXMonitoringBU.Blazor/ -c Release -o ./publish/blazor
```

### Docker
```bash
# Build and run all services with docker-compose (includes SQL Server)
docker-compose up -d

# Build individual images
docker build --target api-publish -t axmonitoring-api:latest .
docker build --target blazor-publish -t axmonitoring-blazor:latest .

# View logs
docker-compose logs -f api
docker-compose logs -f blazor
docker-compose logs -f sqlserver

# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: deletes database data)
docker-compose down -v
```

**Services in docker-compose.yml**:
- `api` - ASP.NET Core Web API on port 7001
- `blazor` - Blazor Server frontend on port 7000
- `sqlserver` - SQL Server 2022 Developer Edition on port 1433
- Network: `axmonitoring-network` connects all services
- Volume: `sqlserver-data` persists SQL Server data

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

**Solution**: `AXMonitoringBU.sln` contains 4 projects:
- `AXMonitoringBU.Api` - Web API backend
- `AXMonitoringBU.Blazor` - Blazor Server frontend
- `AXMonitoringBU.Api.Tests` - API unit/integration tests
- `AXMonitoringBU.Blazor.Tests` - Blazor component tests

### Backend (`AXMonitoringBU.Api/`)
- `Controllers/` - REST API endpoints (23 controllers)
- `Services/` - Business logic layer (31 services)
- `Data/AXDbContext.cs` - EF Core database context
- `Models/` - Domain entities
- `Hubs/MonitoringHub.cs` - SignalR hub for real-time updates
- `BackgroundServices/` - 5 background services for periodic tasks
- `Middleware/` - 3 custom middleware components
- `Swagger/` - API versioning and Swagger configuration
- `Program.cs` - Service registration and middleware configuration

### Frontend (`AXMonitoringBU.Blazor/`)
- `Components/Pages/` - Routable pages (14 pages)
- `Components/Layout/` - MainLayout and NavMenu
- `Components/` - Reusable components (FilterPanel, SearchBox, etc.)
- `Services/` - Client-side API wrappers and state management (DashboardService, PreferencesService, etc.)
- `Models/` - Client-side DTOs and view models
- `wwwroot/` - Static assets (CSS, JS, images)
  - `wwwroot/js/` - JavaScript interop modules (keyboard-shortcuts.js, theme.js)
- `Program.cs` - Service registration and SignalR client setup

**Service Lifecycles**: Most services are Scoped (per-circuit). SignalR services are Singleton to maintain persistent connections across the application.

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

### API Versioning
- **Header-based**: Send `x-api-version: 1.0` or `x-api-version: 2.0` in request headers
- **Query string**: Append `?api-version=1.0` to URLs
- **Default version**: v1.0 (used when not specified)
- **Swagger**: Separate Swagger UI pages for each API version at `/swagger`

### Middleware Pipeline
Custom middleware is configured in `Program.cs` and runs in this order:
1. **CorrelationIdMiddleware** - Adds correlation IDs to requests for distributed tracing
2. **PerformanceMonitoringMiddleware** - Tracks request duration and logs slow requests
3. **RateLimitingMiddleware** - Prevents API abuse with configurable rate limits

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
- **Location**: `AXMonitoringBU.Api/logs/`
  - `axmonitoring-YYYYMMDD.txt` - Human-readable text format
  - `axmonitoring-YYYYMMDD.json` - JSON-structured format for log aggregation
- **Format**: Dual output via Serilog (text + JSON)
- **Levels**: Debug, Information, Warning, Error, Fatal
- **Retention**: 30 days rolling retention

### API Debugging
- **Swagger UI**: https://localhost:7001/swagger
- **Endpoints**: Test all API endpoints interactively
- **Authentication**: Use `/api/auth/login` to get JWT token
- **API Versioning**: Switch between API versions in Swagger dropdown
- **Health Check**: GET `/api/health` for application health status

### Common Issues

**Port Conflicts**: Change ports via `ASPNETCORE_URLS` environment variable or in `Properties/launchSettings.json`. Default ports are 7000 (Blazor), 7001 (API).

**Database Connection**: Verify connection string format and ensure SQL Server is running. Check both Monitoring Database (SQLite/SQL Server) and AX Database (SQL Server) connections.

**SignalR Connection**: Check CORS configuration in API's `Program.cs` and verify hub URL in Blazor's service registration. SignalR client must be Singleton lifecycle to match HubConnection.

**Migration Errors**: Ensure `dotnet-ef` tools are installed globally (`dotnet tool install --global dotnet-ef`). Use `--verbose` flag for detailed output: `dotnet ef database update --verbose --project AXMonitoringBU.Api`

**Missing EF Core Tools**: If migrations fail, update EF Core tools: `dotnet tool update --global dotnet-ef`

## CI/CD

GitHub Actions workflow in `.github/workflows/ci.yml`:
- **Triggers**: Push/PR to main or develop branches
- **Jobs**:
  - `build-api` - Builds and tests API project (tests run with continue-on-error)
  - `build-blazor` - Builds and tests Blazor project (tests run with continue-on-error)
  - `security-scan` - Trivy vulnerability scanner for dependencies + `dotnet list package --vulnerable`
  - `docker-build` - Builds Docker images (runs only on push to main branch)
- **Coverage**: XPlat Code Coverage collection enabled
- **Security**: Trivy results uploaded to GitHub Security tab for vulnerability tracking

## Documentation

- `DEPLOYMENT.md` - Deployment procedures and environment setup
- `MIGRATION_COMPLETE.md` - Python to .NET migration details
- `IMPLEMENTATION_STATUS.md` - Feature checklist and status
- `docs/` - Runbooks and SQL scripts

## Important Implementation Notes

### Authentication & Security
- JWT Bearer token authentication with configurable expiration
- API versioning supports v1 and v2 via header or query string
- Rate limiting middleware prevents API abuse (configurable limits)
- CORS configured for Blazor frontend access

### Real-Time Updates
- SignalR WebSocket connections for push notifications
- Automatic reconnection with exponential backoff (0s, 2s, 10s, 30s)
- Hub endpoint: `/monitoringHub`

### Background Services
All services run as hosted services in the API:
- `MonitoringUpdateService` - Periodic data refresh (default: 30s interval)
- `ArchivingBackgroundService` - Automatic data archiving based on retention policies
- `ScheduledReportBackgroundService` - Scheduled report generation and email delivery
- `DeadlockMonitoringService` - Continuous deadlock detection and graph capture
- `BaselineRecalculationService` - Performance baseline recalculation for trending

### Database Architecture
- **Dual-database pattern**: Monitoring Database (local SQLite/SQL Server) + AX Database (remote SQL Server)
- EF Core code-first migrations for Monitoring Database only
- AX Database is read-only, accessed via `AXDatabaseService`
- SQLite default for easy local development, configurable to SQL Server for production

### UI & Styling
- Blazor Server with interactive server-side rendering
- Bootstrap 5 via Blazorise component library
- Chart.js for data visualization
- Custom CSS in `wwwroot/css/custom.css`
- JavaScript interop for keyboard shortcuts and theme management

### Deployment
- Supports multi-environment deployments (DEV/TST/PROD)
- Environment-specific `appsettings.{Environment}.json` files
- Docker multi-stage builds for optimized container images
- Cross-platform: Windows, Linux, Docker
