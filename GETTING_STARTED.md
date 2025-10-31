# Getting Started - AX Monitor PowerShell Edition

## 🚀 Quick Start (5 Minutes)

### Step 1: Prerequisites Check

```powershell
# Check PowerShell version (need 5.1 or higher)
$PSVersionTable.PSVersion

# Check if you can run scripts
Get-ExecutionPolicy

# If restricted, set to RemoteSigned
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Step 2: Clone and Navigate

```powershell
# Clone the repository
git clone <your-repo-url> AXMonitoringBU
cd AXMonitoringBU\PowerShell
```

### Step 3: Run Installation Wizard

```powershell
# Interactive installation
.\Install-AXMonitor.ps1
```

The wizard will:
- ✅ Check PowerShell version
- ✅ Install Pode module
- ✅ Create configuration files
- ✅ Test database connectivity
- ✅ Set up environment variables

### Step 4: Start the Server

```powershell
# Start with default settings (DEV environment, port 8080)
.\Start-AXMonitor-Working.ps1

# Or specify environment and port
.\Start-AXMonitor-Working.ps1 -Environment TST -Port 9090

# Enable AI features (requires OpenAI API key)
.\Start-AXMonitor-Working.ps1 -EnableOpenAI
```

### Step 5: Test the API

Open a new PowerShell window:

```powershell
# Health check
Invoke-RestMethod -Uri http://localhost:8080/api/health | ConvertTo-Json

# Get KPI data
Invoke-RestMethod -Uri http://localhost:8080/api/kpi | ConvertTo-Json

# Get batch jobs
Invoke-RestMethod -Uri http://localhost:8080/api/batch | ConvertTo-Json

# Get active sessions
Invoke-RestMethod -Uri http://localhost:8080/api/sessions | ConvertTo-Json

# Get alerts
Invoke-RestMethod -Uri http://localhost:8080/api/alerts | ConvertTo-Json
```

Or open in browser:
- http://localhost:8080
- http://localhost:8080/api/health

---

## 📋 Detailed Setup

### Manual Configuration

If you prefer manual setup over the wizard:

#### 1. Install Pode Module

```powershell
Install-Module -Name Pode -Scope CurrentUser -Force
```

#### 2. Create Environment File

```powershell
# Copy example file
Copy-Item -Path ..\env.example -Destination ..\.env.dev

# Edit with your settings
notepad ..\.env.dev
```

Required settings in `.env.dev`:
```ini
# AX Database (Read-Only)
AX_DB_SERVER=your-ax-server
AX_DB_NAME=AX2012_PROD
AX_DB_USER=monitoring_user
AX_DB_PASSWORD=your-password

# Staging Database (Optional)
STAGING_DB_SERVER=your-staging-server
STAGING_DB_NAME=AXMonitoring
STAGING_DB_USER=staging_user
STAGING_DB_PASSWORD=your-password

# SMTP Settings (for alerts)
SMTP_HOST=smtp.yourcompany.com
SMTP_PORT=25
SMTP_FROM=axmonitor@yourcompany.com
ALERT_RECIPIENTS=ops-team@yourcompany.com

# OpenAI API (Optional)
OPENAI_API_KEY=sk-your-api-key
```

#### 3. Test Database Connection

```powershell
# Load the database module
Import-Module .\Modules\AXMonitor.Database\AXMonitor.Database.psm1

# Test connection
$config = @{
    AXDatabase = @{
        Server = "your-server"
        Database = "AX2012_PROD"
        Username = "monitoring_user"
        Password = "your-password"
    }
}

Test-AXDatabaseConnection -Config $config
```

#### 4. Start the Server

```powershell
.\Start-AXMonitor-Working.ps1 -Environment DEV
```

---

## 🔧 Configuration Options

### Command-Line Parameters

```powershell
# Environment selection
.\Start-AXMonitor-Working.ps1 -Environment DEV   # Uses .env.dev
.\Start-AXMonitor-Working.ps1 -Environment TST   # Uses .env.tst
.\Start-AXMonitor-Working.ps1 -Environment PRD   # Uses .env.prd

# Custom port
.\Start-AXMonitor-Working.ps1 -Port 9090

# Enable AI features
.\Start-AXMonitor-Working.ps1 -EnableOpenAI

# Combine options
.\Start-AXMonitor-Working.ps1 -Environment PRD -Port 8080 -EnableOpenAI
```

### Configuration File (config.yaml)

Edit `config.yaml` for advanced settings:

```yaml
monitoring:
  refresh_interval: 60  # seconds
  batch_threshold: 10   # alert if backlog > 10
  
alerts:
  enabled: true
  email_enabled: true
  
logging:
  level: INFO
  file: PowerShell/Logs/axmonitor.log
```

---

## 🎯 Available Endpoints

### Core Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/` | GET | Server status and endpoint list |
| `/api/health` | GET | Health check with system info |
| `/api/kpi` | GET | Key performance indicators |
| `/api/batch` | GET | Batch job monitoring |
| `/api/sessions` | GET | Active user sessions |
| `/api/alerts` | GET | Current alerts |

### Response Format

All endpoints return JSON:

```json
{
  "status": "success",
  "data": { ... },
  "timestamp": "2025-10-27T13:00:00Z"
}
```

---

## 🐛 Troubleshooting

### Issue: "Pode module not found"

```powershell
# Install Pode
Install-Module -Name Pode -Scope CurrentUser -Force

# Verify installation
Get-Module -ListAvailable -Name Pode
```

### Issue: "Cannot connect to database"

```powershell
# Test SQL Server connectivity
Test-NetConnection -ComputerName your-server -Port 1433

# Verify credentials
sqlcmd -S your-server -d AX2012_PROD -U monitoring_user -P your-password

# Check ODBC driver
Get-OdbcDriver | Where-Object {$_.Name -like "*SQL Server*"}
```

### Issue: "Port already in use"

```powershell
# Find process using port 8080
Get-NetTCPConnection -LocalPort 8080 | Select-Object OwningProcess

# Kill the process (replace PID)
Stop-Process -Id <PID> -Force

# Or use a different port
.\Start-AXMonitor-Working.ps1 -Port 9090
```

### Issue: "Execution policy prevents script"

```powershell
# Check current policy
Get-ExecutionPolicy

# Set to RemoteSigned (recommended)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for single execution
powershell.exe -ExecutionPolicy Bypass -File .\Start-AXMonitor-Working.ps1
```

### Enable Detailed Logging

```powershell
# Run with verbose output
.\Start-AXMonitor-Working.ps1 -Verbose

# Check logs
Get-Content .\Logs\axmonitor.log -Tail 50 -Wait
```

---

## 🔐 Security Best Practices

### 1. Never Commit Secrets

```powershell
# .env files are in .gitignore
# Always use .env.example as template
# Never commit actual .env.dev, .env.tst, .env.prd
```

### 2. Use Read-Only Database Account

```sql
-- Create monitoring user with minimal permissions
CREATE LOGIN monitoring_user WITH PASSWORD = 'SecurePassword123!';
USE AX2012_PROD;
CREATE USER monitoring_user FOR LOGIN monitoring_user;
GRANT SELECT ON SCHEMA::dbo TO monitoring_user;
```

### 3. Secure the API

```powershell
# TODO: Add authentication to Pode endpoints
# For now, use firewall rules to restrict access
New-NetFirewallRule -DisplayName "AX Monitor API" `
    -Direction Inbound -LocalPort 8080 -Protocol TCP `
    -Action Allow -RemoteAddress 10.0.0.0/8
```

---

## 📦 Windows Service Installation

For production deployment:

```powershell
# Run the service installer
.\Install-Service.ps1

# Or manually with NSSM
choco install nssm

$psPath = (Get-Command pwsh).Source
$scriptPath = "C:\apps\ax-monitor\PowerShell\Start-AXMonitor-Working.ps1"

nssm install AXMonitor $psPath
nssm set AXMonitor AppParameters "-ExecutionPolicy Bypass -File `"$scriptPath`" -Environment PRD"
nssm set AXMonitor AppDirectory "C:\apps\ax-monitor\PowerShell"
nssm set AXMonitor Start SERVICE_AUTO_START

# Start the service
Start-Service AXMonitor

# Check status
Get-Service AXMonitor
```

---

## 📚 Next Steps

1. **Configure Alerts**: Edit alert rules in `config.yaml`
2. **Set Up Monitoring**: Configure KPI thresholds
3. **Enable AI**: Add OpenAI API key for intelligent insights
4. **Create Dashboard**: Build a frontend to consume the REST API
5. **Add Tests**: Write Pester tests for modules

---

## 🆘 Getting Help

- **Documentation**: See `PowerShell/README.md`
- **Module Help**: `Get-Help <CommandName> -Full`
- **Issues**: Check GitHub issues
- **Logs**: `PowerShell/Logs/axmonitor.log`

---

## ✅ Verification Checklist

After setup, verify:

- [ ] PowerShell 5.1+ installed
- [ ] Pode module installed
- [ ] Environment file created
- [ ] Database connection successful
- [ ] Server starts without errors
- [ ] API endpoints respond
- [ ] Logs are being written

---

**You're all set! The AX Monitor is now running on PowerShell.** 🎉
