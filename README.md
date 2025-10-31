# AX 2012 R3 Performance Monitor - PowerShell Edition

**Real-time monitoring and alerting REST API for Microsoft Dynamics AX 2012 R3 performance issues**

---

## Overview

The AX Performance Monitor is a **pure PowerShell-based** monitoring solution that provides operations teams with real-time visibility into AX 2012 R3 and SQL Server 2016 performance metrics. Built on the **Pode web framework**, it exposes REST API endpoints for monitoring batch jobs, user sessions, SQL blocking chains, and database health.

### Key Features

- **✅ Pure PowerShell:** No Python dependencies, native Windows integration
- **🚀 Pode Web Server:** Modern, fast REST API framework for PowerShell
- **📊 Real-time Monitoring:** Batch jobs, sessions, blocking chains, SQL health
- **🔔 Proactive Alerting:** Threshold-based alerts with email notifications
- **🤖 AI Integration:** Optional OpenAI API integration for intelligent insights
- **⚙️ Modular Architecture:** Clean separation of concerns with PowerShell modules
- **🔧 Easy Deployment:** Windows Service support, no virtual environments needed

---

## Quick Start

### Prerequisites

- **Windows PowerShell 5.1+** or **PowerShell 7+**
- **SQL Server ODBC Driver 17 or 18**
- Access to AX 2012 R3 database (read-only)
- Separate staging database for metrics storage (optional)

### Installation

```powershell
# Clone repository
git clone <repo-url> AXMonitoringBU
cd AXMonitoringBU

# Navigate to PowerShell directory
cd PowerShell

# Run installation wizard
.\Install-AXMonitor.ps1

# Start the server
.\Start-AXMonitor-Working.ps1 -Environment DEV -Port 8080
```

The REST API will be available at `http://localhost:8080`

### Quick Test

```powershell
# Health check
Invoke-RestMethod -Uri http://localhost:8080/api/health

# Get KPI data
Invoke-RestMethod -Uri http://localhost:8080/api/kpi

# Get batch jobs
Invoke-RestMethod -Uri http://localhost:8080/api/batch
```

---

## Project Structure

```
AXMonitoringBU/
├── PowerShell/                    # Main PowerShell application
│   ├── Modules/                   # PowerShell modules
│   │   ├── AXMonitor.Config/     # Configuration management
│   │   ├── AXMonitor.Database/   # Database access layer
│   │   ├── AXMonitor.Monitoring/ # Monitoring functions
│   │   ├── AXMonitor.Alerts/     # Alert system
│   │   └── AXMonitor.AI/         # AI integration (optional)
│   ├── Config/                    # Configuration files
│   ├── Views/                     # HTML templates (optional)
│   ├── Public/                    # Static files (CSS, JS)
│   ├── Logs/                      # Application logs
│   ├── Start-AXMonitor-Working.ps1  # Main server script
│   ├── Install-AXMonitor.ps1      # Installation wizard
│   └── Install-Service.ps1        # Windows Service installer
├── docs/                          # Documentation
│   ├── prd.md                     # Product Requirements Document
│   ├── architecture.md            # Architecture overview
│   └── stories/                   # Epic and story definitions
├── config.yaml                    # Main configuration file
├── .env.example                   # Environment variables template
├── MIGRATION_TO_POWERSHELL.md     # Migration documentation
└── README.md                      # This file
```

See `PowerShell/README.md` for detailed PowerShell documentation.

---

## Documentation

### Core Documents

- **[PowerShell Quick Start](PowerShell/QUICKSTART.md)** - Get started in 5 minutes
- **[PowerShell Project Summary](PowerShell/PROJECT_SUMMARY.md)** - Detailed PowerShell implementation
- **[Migration Guide](MIGRATION_TO_POWERSHELL.md)** - Python to PowerShell migration details
- **[Product Requirements Document (PRD)](docs/prd.md)** - Feature requirements, user stories
- **[Architecture Document](docs/architecture.md)** - System architecture, component design

### PowerShell Modules

Each module is self-contained with its own documentation:

- **AXMonitor.Config** - Configuration and environment management
- **AXMonitor.Database** - SQL Server connectivity and queries
- **AXMonitor.Monitoring** - KPI collection and monitoring functions
- **AXMonitor.Alerts** - Alert rules and notification system
- **AXMonitor.AI** - OpenAI integration for intelligent insights

---

## Development

### PowerShell Development

```powershell
# Test modules
cd PowerShell
.\Test-Modules.ps1

# Run with different environments
.\Start-AXMonitor-Working.ps1 -Environment DEV
.\Start-AXMonitor-Working.ps1 -Environment TST
.\Start-AXMonitor-Working.ps1 -Environment PRD

# Enable AI features
.\Start-AXMonitor-Working.ps1 -EnableOpenAI

# Custom port
.\Start-AXMonitor-Working.ps1 -Port 9090
```

### Code Quality

PowerShell best practices:

```powershell
# Use PSScriptAnalyzer for linting
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser
Invoke-ScriptAnalyzer -Path .\PowerShell -Recurse

# Format with PowerShell Beautifier
Install-Module -Name PowerShell-Beautifier -Scope CurrentUser
```

### Testing with Pester

```powershell
# Install Pester
Install-Module -Name Pester -Scope CurrentUser -Force

# Run tests (when implemented)
Invoke-Pester -Path .\PowerShell\Tests
```

---

## Configuration

### Environment Variables

Configuration is managed through `.env` files:

```powershell
# Development
cp .env.example .env.dev
# Edit .env.dev with DEV credentials

# Test
cp .env.example .env.tst
# Edit .env.tst with TST credentials

# Production
cp .env.example .env.prd
# Edit .env.prd with PRD credentials
```

Set `APP_ENV` to select environment:
```powershell
$env:APP_ENV="DEV"    # Uses .env.dev
$env:APP_ENV="TST"    # Uses .env.tst
$env:APP_ENV="PRD"    # Uses .env.prd
```

See `.env.example` for all required variables.

### Required Environment Variables

- **AX Database:** `AX_DB_SERVER`, `AX_DB_NAME`, `AX_DB_USER`, `AX_DB_PASSWORD`
- **Staging Database:** `STAGING_DB_SERVER`, `STAGING_DB_NAME`, `STAGING_DB_USER`, `STAGING_DB_PASSWORD`
- **SMTP:** `SMTP_HOST`, `SMTP_PORT`, `SMTP_FROM`
- **Alerts:** `ALERT_RECIPIENTS` (comma-separated emails)

---

## Deployment

### Windows Service Deployment

```powershell
# Navigate to PowerShell directory
cd PowerShell

# Run the service installer
.\Install-Service.ps1

# Or manually with NSSM
choco install nssm

# Install service
$psPath = (Get-Command pwsh).Source  # or powershell.exe
$scriptPath = "C:\apps\ax-monitor\PowerShell\Start-AXMonitor-Working.ps1"

nssm install AXMonitor $psPath
nssm set AXMonitor AppParameters "-ExecutionPolicy Bypass -File `"$scriptPath`" -Environment PRD"
nssm set AXMonitor AppDirectory "C:\apps\ax-monitor\PowerShell"
nssm set AXMonitor DisplayName "AX Monitor Service"
nssm set AXMonitor Description "AX 2012 R3 Performance Monitoring Service"

# Start service
nssm start AXMonitor
```

### Manual Deployment

```powershell
# Copy files to server
Copy-Item -Path .\PowerShell -Destination C:\apps\ax-monitor\ -Recurse

# Configure environment
Copy-Item -Path .env.example -Destination C:\apps\ax-monitor\.env.prd
# Edit .env.prd with production credentials

# Test the server
cd C:\apps\ax-monitor\PowerShell
.\Start-AXMonitor-Working.ps1 -Environment PRD
```

---

## Architecture Overview

### Technology Stack

- **Language:** PowerShell 5.1+ / PowerShell 7+
- **Web Framework:** Pode (PowerShell web framework)
- **Database:** SQL Server 2016 (System.Data.SqlClient)
- **Scheduler:** Pode Timers / PowerShell Scheduled Jobs
- **API:** REST API with JSON responses
- **Logging:** Pode logging + PowerShell Write-Host

### System Components

1. **Web Layer:** Pode REST API server
2. **Module Layer:** PowerShell modules for business logic
   - `AXMonitor.Config` - Configuration management
   - `AXMonitor.Database` - Database access
   - `AXMonitor.Monitoring` - Monitoring functions
   - `AXMonitor.Alerts` - Alert engine
   - `AXMonitor.AI` - AI integration
3. **Data Access Layer:** SQL Server connectivity via .NET SqlClient
4. **Alerting Layer:** Rule engine and email notifications

### Data Flow

```
AX 2012 DB (Read-Only) → PowerShell Modules → REST API Endpoints
                                                     ↓
Users/Apps → HTTP Requests → Pode Server → JSON Responses
                                        ↓
                                  Alert Engine → Email (SMTP)
```

### API Endpoints

- `GET /` - Server status and endpoint list
- `GET /api/health` - Health check with system info
- `GET /api/kpi` - Key performance indicators
- `GET /api/batch` - Batch job monitoring
- `GET /api/sessions` - Active user sessions
- `GET /api/alerts` - Alert status

See `PowerShell/README.md` for complete architecture documentation.

---

## Performance Targets

| Metric | Target (p95) |
|--------|--------------|
| API response time | < 500ms |
| Data freshness | < 60s |
| Database query latency | < 10s |
| Server startup time | < 5s |

---

## Contributing

### Development Workflow

1. Create feature branch: `git checkout -b feature/your-feature`
2. Implement changes following PowerShell best practices
3. Write/update Pester tests
4. Run quality checks: `Invoke-ScriptAnalyzer`
5. Commit with conventional commit message: `feat(batch): add P95 calculation`
6. Push and create pull request

### Code Review Checklist

- [ ] Code follows PowerShell best practices
- [ ] All functions have comment-based help
- [ ] No hardcoded secrets or credentials
- [ ] All SQL queries are parameterized
- [ ] Pester tests added/updated
- [ ] Documentation updated if needed
- [ ] Module manifests updated (if applicable)

---

## License

See [LICENSE](LICENSE) file for details.

---

## Support & Contact

For issues, questions, or feature requests:
- **Issues:** [GitHub Issues](<repo-url>/issues)
- **Documentation:** See `docs/` directory
- **Email:** ops-team@corp.local

---

## Acknowledgments

Built with [Pode](https://badgerati.github.io/Pode/) - A powerful PowerShell web framework for building REST APIs and web applications.

### Why PowerShell?

- ✅ **Native Windows Integration** - No additional runtime required
- ✅ **Direct .NET Access** - Full access to .NET Framework libraries
- ✅ **SQL Server Optimized** - Native SQL Server connectivity
- ✅ **Simple Deployment** - No virtual environments or package managers
- ✅ **Enterprise Ready** - Built-in Windows Service support
- ✅ **Lightweight** - Minimal dependencies, fast startup

### Migration from Python

This project was originally built with Python/Streamlit. See [MIGRATION_TO_POWERSHELL.md](MIGRATION_TO_POWERSHELL.md) for details on the migration process and rationale.
