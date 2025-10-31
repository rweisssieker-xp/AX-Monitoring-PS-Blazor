# Quick Start Guide - AX Monitor PowerShell Edition

Get up and running in 5 minutes!

## Step 1: Install (2 minutes)

```powershell
cd PowerShell
.\Install-AXMonitor.ps1 -Environment DEV
```

## Step 2: Configure (2 minutes)

Edit `Config\.env.DEV`:

```ini
AX_DB_SERVER=your-server
AX_DB_NAME=AX2012R3_PROD
STAGING_DB_SERVER=your-staging-server
STAGING_DB_NAME=AXMonitor_Staging
SMTP_SERVER=smtp.office365.com
ALERT_RECIPIENTS=you@domain.com
```

## Step 3: Start (1 minute)

```powershell
.\Start-AXMonitor.ps1 -Environment DEV
```

## Step 4: Access

Open browser: **http://localhost:8080**

Done! 🎉

---

## Enable AI Features (Optional)

Add to config:
```ini
OPENAI_ENABLED=true
OPENAI_API_KEY=sk-your-openai-key
```

Start with AI:
```powershell
.\Start-AXMonitor.ps1 -Environment DEV -EnableOpenAI
```

---

## Troubleshooting

**Database connection failed?**
- Check server name and credentials
- Verify ODBC driver is installed
- Test with Windows Authentication (leave user/password blank)

**Port already in use?**
```powershell
.\Start-AXMonitor.ps1 -Environment DEV -Port 9090
```

**Need help?**
See full README.md for detailed documentation.
