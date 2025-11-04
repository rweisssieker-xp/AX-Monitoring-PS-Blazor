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

