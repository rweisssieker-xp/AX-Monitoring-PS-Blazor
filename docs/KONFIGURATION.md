# Konfiguration - AOS und Datenbank einstellen

## Übersicht

Die Konfiguration erfolgt über **zwei Möglichkeiten**:

1. **Environment Variables** (empfohlen für Production)
2. **appsettings.json** (für Development)

## 1. Datenbank-Konfiguration

### Windows Authentication (Empfohlen)

Wenn Sie Windows Authentication verwenden möchten, setzen Sie `UseWindowsAuthentication` auf `true`:

**Option A: Environment Variables**
```bash
$env:USE_WINDOWS_AUTH="true"
$env:DB_SERVER="SQLSERVER01"
$env:DB_NAME="AX2012R3"
```

**Option B: appsettings.json**
```json
{
  "Database": {
    "Server": "SQLSERVER01",
    "Name": "AX2012R3",
    "UseWindowsAuthentication": "true"
  }
}
```

Die Connection String wird automatisch mit `Integrated Security=true` erstellt.

### SQL Server Authentication

**Option A: Environment Variables (Production)**

Erstellen Sie eine `.env` Datei im Projekt-Root oder setzen Sie System-Environment-Variablen:

```bash
# Windows PowerShell
$env:USE_WINDOWS_AUTH="false"
$env:DB_SERVER="SQLSERVER01"
$env:DB_NAME="AX2012R3"
$env:DB_USER="ax_ro"
$env:DB_PASSWORD="IhrPasswort"
$env:DB_DRIVER="ODBC Driver 17 for SQL Server"
```

**Option B: appsettings.json (Development)**

Bearbeiten Sie `AXMonitoringBU.Api/appsettings.Development.json`:

```json
{
  "Database": {
    "Provider": "SqlServer",
    "Server": "SQLSERVER01",
    "Name": "AX2012R3",
    "User": "ax_ro",
    "Password": "IhrPasswort",
    "UseWindowsAuthentication": "false",
    "Driver": "ODBC Driver 17 for SQL Server",
    "ConnectionTimeout": 30,
    "CommandTimeout": 60
  }
}
```

**Wichtig**: Für Production verwenden Sie `appsettings.json` mit Environment-Variable-Platzhaltern:

```json
{
  "Database": {
    "Server": "${DB_SERVER}",
    "Name": "${DB_NAME}",
    "User": "${DB_USER}",
    "Password": "${DB_PASSWORD}"
  }
}
```

## 2. AOS-Server-Konfiguration

### Option A: Environment Variables

```bash
$env:AOS_SERVER_1="AOS01"
$env:AOS_SERVER_2="AOS02"
```

### Option B: appsettings.json

Bearbeiten Sie `AXMonitoringBU.Api/appsettings.Development.json` oder `appsettings.json`:

```json
{
  "AOS": {
    "Servers": [
      "AOS01",
      "AOS02",
      "AOS03"
    ],
    "DefaultServer": "AOS01",
    "ConnectionTimeout": 30
  }
}
```

Für Production mit Environment Variables:

```json
{
  "AOS": {
    "Servers": [
      "${AOS_SERVER_1}",
      "${AOS_SERVER_2}"
    ],
    "DefaultServer": "${AOS_SERVER_1}",
    "ConnectionTimeout": 30
  }
}
```

## Konfigurationsdateien im Überblick

### API-Konfiguration (`AXMonitoringBU.Api/`)

- **`appsettings.json`** - Production-Konfiguration (mit Environment-Variable-Platzhaltern)
- **`appsettings.Development.json`** - Development-Konfiguration (direkte Werte)

### Blazor-Konfiguration (`AXMonitoringBU.Blazor/`)

- **`appsettings.json`** - Production-Konfiguration
- **`appsettings.Development.json`** - Development-Konfiguration

### Root-Verzeichnis

- **`env.example`** - Vorlage für Environment Variables

## Environment Variables Mapping

Die folgende Tabelle zeigt das Mapping zwischen Environment Variables und `appsettings.json` Pfaden:

| Environment Variable | appsettings.json Path | Beschreibung |
|---------------------|----------------------|--------------|
| `AX_DB_SERVER` | `Database:Server` | SQL Server Hostname |
| `AX_DB_NAME` | `Database:Name` | Datenbankname |
| `AX_DB_USER` | `Database:User` | Datenbankbenutzer |
| `AX_DB_PASSWORD` | `Database:Password` | Datenbankpasswort |
| `AX_USE_WINDOWS_AUTH` | `Database:UseWindowsAuthentication` | Windows Authentication verwenden |
| `DB_DRIVER` | `Database:Driver` | ODBC Driver Name |
| `DB_CONNECTION_STRING` | `ConnectionStrings:DefaultConnection` | Vollständiger Connection String |
| `MONITORING_DB_CONNECTION_STRING` | `MonitoringDatabase:ConnectionString` | Monitoring DB Connection String |
| `AOS_SERVER_1` | `AOS:Servers[0]` | Erster AOS Server |
| `AOS_SERVER_2` | `AOS:Servers[1]` | Zweiter AOS Server |
| `SMTP_SERVER` | `Alerts:Email:SmtpServer` | SMTP Server Hostname |
| `SMTP_PORT` | `Alerts:Email:SmtpPort` | SMTP Port |
| `SMTP_USER` | `Alerts:Email:SmtpUser` | SMTP Benutzer |
| `SMTP_PASSWORD` | `Alerts:Email:SmtpPassword` | SMTP Passwort |
| `EMAIL_FROM` | `Alerts:Email:FromAddress` | Absender E-Mail |
| `TEAMS_CRITICAL_WEBHOOK_URL` | `Alerts:Teams:CriticalWebhookUrl` | Teams Critical Webhook |
| `TEAMS_WARNING_WEBHOOK_URL` | `Alerts:Teams:WarningWebhookUrl` | Teams Warning Webhook |
| `TEAMS_INFO_WEBHOOK_URL` | `Alerts:Teams:InfoWebhookUrl` | Teams Info Webhook |
| `SECRET_KEY` | `Security:SecretKey` | Secret Key für Verschlüsselung |
| `JWT_SECRET` | `Security:JwtSecret` | JWT Secret Key |
| `OPENAI_API_KEY` | `OpenAI:ApiKey` | OpenAI API Key |
| `OPENAI_ANALYSIS_ENABLED` | `OpenAI:AnalysisEnabled` | OpenAI Analysis aktivieren |
| `TICKETING_SYSTEM` | `Integrations:Ticketing:DefaultSystem` | Standard Ticketing System |
| `SERVICENOW_URL` | `Integrations:Ticketing:ServiceNow:BaseUrl` | ServiceNow URL |
| `JIRA_URL` | `Integrations:Ticketing:Jira:BaseUrl` | Jira URL |
| `AZURE_DEVOPS_URL` | `Integrations:Ticketing:AzureDevOps:BaseUrl` | Azure DevOps URL |
| `CORS_ALLOWED_ORIGIN_1` | `Cors:AllowedOrigins[0]` | Erste erlaubte CORS Origin |
| `CORS_ALLOWED_ORIGIN_2` | `Cors:AllowedOrigins[1]` | Zweite erlaubte CORS Origin |

### Verwendung in Production

In Production sollten Sie Environment Variables verwenden. Die `appsettings.json` Datei verwendet Platzhalter im Format `${VARIABLE_NAME}`:

```json
{
  "Database": {
    "Server": "${AX_DB_SERVER}",
    "Name": "${AX_DB_NAME}",
    "User": "${AX_DB_USER}",
    "Password": "${AX_DB_PASSWORD}"
  }
}
```

Diese Platzhalter werden zur Laufzeit durch die entsprechenden Environment Variables ersetzt.

## Schnellstart für Development

1. Öffnen Sie `AXMonitoringBU.Api/appsettings.Development.json`
2. Aktualisieren Sie die `Database`-Sektion:
   ```json
   "Database": {
     "Provider": "SqlServer",
     "Server": "IhrSQLServer",
     "Name": "AX2012R3",
     "User": "ax_ro",
     "Password": "IhrPasswort"
   }
   ```
3. Aktualisieren Sie die `AOS`-Sektion:
   ```json
   "AOS": {
     "Servers": ["AOS01", "AOS02"],
     "DefaultServer": "AOS01"
   }
   ```

## Windows-Benutzer durchreichen

Der Windows-Benutzer wird automatisch erkannt und verwendet:

- **Alerts**: Der `CreatedBy`-Feld wird automatisch mit dem Windows-Benutzer befüllt
- **API-Endpoint**: `GET /api/v1/auth/current-user` zeigt den aktuellen Windows-Benutzer
- **Login**: Wenn kein Username/Password angegeben wird, wird automatisch der Windows-Benutzer verwendet

Der Windows-Benutzer wird aus folgenden Quellen ermittelt (in dieser Reihenfolge):
1. HTTP Context (falls Windows Authentication aktiviert ist)
2. Current Windows Identity
3. Environment UserName

## Konfiguration testen

Nach dem Start können Sie die Konfiguration überprüfen:

- Database Info: https://localhost:7001/api/v1/database/info
- Current User: https://localhost:7001/api/v1/auth/current-user
- Health Check: https://localhost:7001/health

Die konfigurierte Datenbank wird automatisch im Header der Blazor-App angezeigt.

