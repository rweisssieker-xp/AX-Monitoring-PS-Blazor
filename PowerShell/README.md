# AX 2012 R3 Performance Monitor - PowerShell Edition

**Real-time monitoring and alerting system for Microsoft Dynamics AX 2012 R3 built with PowerShell and Pode**

---

## 🌟 Features

### Core Monitoring
- **Batch Job Monitoring** - Real-time tracking of batch jobs, execution times, error rates
- **Session Management** - Active/idle session tracking, user activity monitoring
- **Blocking Analysis** - SQL blocking chain detection with root cause analysis
- **SQL Health Metrics** - CPU, memory, connections, query performance
- **Proactive Alerting** - Email, Microsoft Teams, and Slack notifications

### AI-Powered Features (Optional)
- **🤖 AI Chat Assistant** - Natural language queries about system performance
- **📊 Anomaly Detection** - AI-powered detection of unusual patterns
- **🔮 Predictive Analysis** - Forecast potential issues before they occur
- **💡 Smart Recommendations** - AI-generated optimization suggestions

### Modern Web Interface
- **Responsive Dashboard** - Real-time KPI cards and charts
- **Interactive Charts** - CPU/Memory trends, batch job status
- **Alert Management** - View, acknowledge, and track alerts
- **Dark/Light Themes** - Modern, professional UI

---

## 📋 Prerequisites

- **PowerShell 7+** - [Download](https://github.com/PowerShell/PowerShell/releases)
- **SQL Server ODBC Driver 17 or 18** - [Download](https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)
- **Access to AX 2012 R3 Database** (read-only)
- **Staging Database** for metrics storage (read/write)
- **OpenAI API Key** (optional, for AI features)

---

## 🚀 Quick Start

### 1. Installation

```powershell
# Clone or download the repository
cd PowerShell

# Run installation script
.\Install-AXMonitor.ps1 -Environment DEV
```

The installer will:
- ✅ Check PowerShell version
- ✅ Install Pode module
- ✅ Create directory structure
- ✅ Generate configuration template
- ✅ Initialize staging database

### 2. Configuration

Edit the configuration file for your environment:

```powershell
notepad Config\.env.DEV
```

**Required Settings:**
```ini
# AX Database (READ-ONLY)
AX_DB_SERVER=your-ax-server
AX_DB_NAME=AX2012R3_PROD
AX_DB_USER=monitoring_user
AX_DB_PASSWORD=your-password

# Staging Database
STAGING_DB_SERVER=your-staging-server
STAGING_DB_NAME=AXMonitor_Staging
STAGING_DB_USER=staging_user
STAGING_DB_PASSWORD=your-password

# Email Alerts
SMTP_SERVER=smtp.office365.com
SMTP_PORT=587
SMTP_USER=alerts@yourdomain.com
SMTP_PASSWORD=your-smtp-password
ALERT_RECIPIENTS=ops-team@yourdomain.com
```

**Optional AI Settings:**
```ini
OPENAI_ENABLED=true
OPENAI_API_KEY=sk-your-api-key
OPENAI_MODEL=gpt-4
```

### 3. Start the Server

```powershell
# Start with default settings (port 8080)
.\Start-AXMonitor.ps1 -Environment DEV

# Start with AI features enabled
.\Start-AXMonitor.ps1 -Environment DEV -EnableOpenAI

# Start on custom port
.\Start-AXMonitor.ps1 -Environment DEV -Port 9090
```

### 4. Access the Dashboard

Open your browser to: **http://localhost:8080**

---

## 📁 Project Structure

```
PowerShell/
├── Start-AXMonitor.ps1          # Main entry point
├── Install-AXMonitor.ps1        # Installation script
├── README.md                    # This file
│
├── Modules/                     # PowerShell modules
│   ├── AXMonitor.Config/        # Configuration management
│   ├── AXMonitor.Database/      # Database connectivity
│   ├── AXMonitor.Monitoring/    # Monitoring services
│   ├── AXMonitor.Alerts/        # Alerting system
│   └── AXMonitor.AI/            # AI/OpenAI integration
│
├── Views/                       # HTML templates
│   ├── index.html               # Dashboard
│   ├── batch.html               # Batch jobs page
│   ├── sessions.html            # Sessions page
│   ├── blocking.html            # Blocking analysis
│   ├── sql-health.html          # SQL health page
│   ├── alerts.html              # Alerts page
│   └── ai-assistant.html        # AI assistant
│
├── Public/                      # Static assets
│   ├── css/
│   │   └── styles.css           # Main stylesheet
│   └── js/
│       └── dashboard.js         # Dashboard JavaScript
│
├── Config/                      # Configuration files
│   ├── env.example              # Configuration template
│   ├── .env.DEV                 # Development config
│   ├── .env.TST                 # Test config
│   └── .env.PRD                 # Production config
│
└── Logs/                        # Application logs
```

---

## 🔧 Configuration Reference

### Database Settings

| Setting | Description | Required |
|---------|-------------|----------|
| `AX_DB_SERVER` | AX database server | ✅ |
| `AX_DB_NAME` | AX database name | ✅ |
| `AX_DB_USER` | Database username | ⚠️ |
| `AX_DB_PASSWORD` | Database password | ⚠️ |
| `STAGING_DB_SERVER` | Staging server | ✅ |
| `STAGING_DB_NAME` | Staging database | ✅ |

⚠️ Leave blank to use Windows Authentication (Trusted_Connection)

### Monitoring Thresholds

| Setting | Default | Description |
|---------|---------|-------------|
| `THRESHOLD_CPU` | 80 | CPU usage alert threshold (%) |
| `THRESHOLD_MEMORY` | 85 | Memory usage alert threshold (%) |
| `THRESHOLD_BLOCKING` | 30 | Blocking duration threshold (seconds) |
| `MONITORING_INTERVAL` | 5 | Data collection interval (minutes) |
| `RETENTION_DAYS` | 90 | Data retention period (days) |

### Alert Channels

**Email:**
```ini
ALERT_EMAIL_ENABLED=true
SMTP_SERVER=smtp.office365.com
SMTP_PORT=587
SMTP_USE_SSL=true
ALERT_RECIPIENTS=team@domain.com,admin@domain.com
```

**Microsoft Teams:**
```ini
ALERT_TEAMS_ENABLED=true
TEAMS_WEBHOOK_URL=https://outlook.office.com/webhook/...
```

**Slack:**
```ini
ALERT_SLACK_ENABLED=true
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/...
```

---

## 🤖 AI Features

### Enabling AI

1. Get an OpenAI API key from [platform.openai.com](https://platform.openai.com)
2. Configure in `.env` file:
```ini
OPENAI_ENABLED=true
OPENAI_API_KEY=sk-your-api-key
OPENAI_MODEL=gpt-4
```
3. Start server with AI enabled:
```powershell
.\Start-AXMonitor.ps1 -Environment DEV -EnableOpenAI
```

### AI Capabilities

**Chat Assistant** (`/ai-assistant`)
- Ask questions about system performance
- Get explanations of metrics and alerts
- Receive troubleshooting guidance

**Anomaly Detection** (automatic)
- Detects unusual patterns in metrics
- Uses statistical analysis + AI interpretation
- Creates alerts for critical anomalies

**Predictive Analysis** (runs every 6 hours)
- Forecasts potential issues
- Analyzes trends over time
- Provides proactive recommendations

**Smart Recommendations** (on-demand)
- System optimization suggestions
- Resource allocation advice
- Best practices guidance

---

## 📊 REST API

### Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Health check |
| `/api/kpi` | GET | KPI summary |
| `/api/batch` | GET | Batch jobs |
| `/api/sessions` | GET | Active sessions |
| `/api/blocking` | GET | Blocking chains |
| `/api/sql-health` | GET | SQL health metrics |
| `/api/alerts` | GET | Alerts list |
| `/api/alerts/:id/acknowledge` | POST | Acknowledge alert |
| `/api/ai/chat` | POST | AI chat (if enabled) |
| `/api/ai/anomalies` | GET | AI anomalies (if enabled) |
| `/api/ai/recommendations` | GET | AI recommendations (if enabled) |

### Example Usage

```powershell
# Get KPI data
$kpi = Invoke-RestMethod -Uri 'http://localhost:8080/api/kpi'

# Get batch jobs
$batches = Invoke-RestMethod -Uri 'http://localhost:8080/api/batch'

# AI Chat
$response = Invoke-RestMethod -Uri 'http://localhost:8080/api/ai/chat' `
    -Method Post `
    -Body (@{ message = "Why is CPU usage high?" } | ConvertTo-Json) `
    -ContentType 'application/json'
```

---

## 🪟 Windows Service Installation

To run AX Monitor as a Windows Service:

### Using NSSM (Recommended)

1. Download NSSM from [nssm.cc](https://nssm.cc/download)
2. Run the service installation script:

```powershell
.\Install-Service.ps1
```

3. Start the service:

```powershell
nssm start AXMonitor
```

### Manual Service Management

```powershell
# Start service
nssm start AXMonitor

# Stop service
nssm stop AXMonitor

# Restart service
nssm restart AXMonitor

# Remove service
nssm remove AXMonitor confirm
```

---

## 🔍 Troubleshooting

### Database Connection Issues

**Error: "Database connection failed"**

1. Check ODBC driver is installed:
```powershell
Get-OdbcDriver | Where-Object { $_.Name -like '*SQL Server*' }
```

2. Test connection manually:
```powershell
$conn = New-Object System.Data.Odbc.OdbcConnection("Driver={ODBC Driver 17 for SQL Server};Server=yourserver;Database=yourdb;Trusted_Connection=yes;")
$conn.Open()
$conn.Close()
```

3. Verify firewall allows SQL Server port (1433)

### Module Import Errors

**Error: "Module not found"**

```powershell
# Reinstall modules
.\Install-AXMonitor.ps1 -Environment DEV -SkipDatabaseInit
```

### Port Already in Use

**Error: "Address already in use"**

```powershell
# Use different port
.\Start-AXMonitor.ps1 -Environment DEV -Port 9090
```

### AI Features Not Working

1. Verify API key is correct
2. Check internet connectivity
3. Review OpenAI API usage limits
4. Check logs in `Logs/` directory

---

## 📈 Performance Tuning

### Monitoring Interval

Adjust collection frequency in `.env`:
```ini
MONITORING_INTERVAL=5  # Minutes (default: 5)
```

Lower values = more frequent updates, higher load

### Data Retention

Configure retention period:
```ini
RETENTION_DAYS=90  # Days (default: 90)
```

### Database Optimization

1. Create indexes on staging database:
```sql
CREATE INDEX IX_Metrics_Timestamp ON Metrics(Timestamp)
CREATE INDEX IX_Alerts_Status ON Alerts(Status)
```

2. Schedule regular maintenance:
```sql
-- Rebuild indexes weekly
ALTER INDEX ALL ON Metrics REBUILD
```

---

## 🔐 Security Best Practices

1. **Use Read-Only Account for AX Database**
   - Create dedicated monitoring user
   - Grant only SELECT permissions

2. **Secure Configuration Files**
   - Never commit `.env.*` files to source control
   - Use Windows file permissions to restrict access

3. **Enable Authentication** (optional)
```ini
ENABLE_AUTH=true
JWT_SECRET=your-secure-secret
```

4. **Use HTTPS in Production**
   - Configure reverse proxy (IIS, nginx)
   - Use SSL certificates

5. **Restrict Network Access**
   - Bind to specific IP if needed
   - Use firewall rules

---

## 🆚 Comparison: Python vs PowerShell Edition

| Feature | Python/Streamlit | PowerShell/Pode |
|---------|------------------|-----------------|
| **Language** | Python 3.10+ | PowerShell 7+ |
| **Web Framework** | Streamlit | Pode |
| **REST API** | Limited | Full REST API |
| **Windows Integration** | Good | Excellent |
| **Performance** | Good | Excellent |
| **Deployment** | pip/venv | Native modules |
| **Service Installation** | NSSM | NSSM/Native |
| **AI Integration** | ✅ | ✅ |
| **Learning Curve** | Low | Medium |

---

## 📝 Development

### Adding New Monitoring Metrics

1. Add function to `Modules/AXMonitor.Monitoring/`
2. Create API endpoint in `Start-AXMonitor.ps1`
3. Update UI in `Views/` and `Public/js/`

### Adding New Alert Rules

Edit `Modules/AXMonitor.Alerts/AXMonitor.Alerts.psm1`:

```powershell
function Invoke-AXAlertCheck {
    # Add your custom rule
    if ($customCondition) {
        New-AXAlert -Config $Config `
            -AlertType 'CustomAlert' `
            -Severity 'Warning' `
            -Message "Custom condition detected"
    }
}
```

---

## 🤝 Contributing

Contributions are welcome! Areas for improvement:

- Additional monitoring metrics
- New visualization types
- Enhanced AI capabilities
- Performance optimizations
- Documentation improvements

---

## 📄 License

See [LICENSE](../LICENSE) file for details.

---

## 🙏 Acknowledgments

Built with:
- [Pode](https://badgerati.github.io/Pode/) - PowerShell web framework
- [Chart.js](https://www.chartjs.org/) - JavaScript charting
- [OpenAI](https://openai.com/) - AI capabilities

---

## 📞 Support

For issues, questions, or feature requests:
- **Issues**: Create a GitHub issue
- **Documentation**: See `docs/` directory
- **Email**: ops-team@yourdomain.com

---

**Made with ❤️ for the AX community**
