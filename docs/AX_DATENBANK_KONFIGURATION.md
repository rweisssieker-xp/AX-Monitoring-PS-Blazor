# AX-Datenbank Konfiguration

## Wichtig: Zwei Datenbanken

Die Anwendung verwendet **zwei verschiedene Datenbanken**:

1. **AX-Datenbank** (AOS-Datenbank) - Hier werden Batch Jobs, Sessions, etc. gelesen
2. **Monitoring-Datenbank** (lokal) - Hier wird die Historie gespeichert (SQLite)

## 1. AX-Datenbank konfigurieren

**Datei:** `AXMonitoringBU.Api/appsettings.Development.json`

```json
{
  "Database": {
    "Provider": "SqlServer",
    "Server": "IhrAXSQLServer",           // z.B. "SQLSERVER01"
    "Name": "IhrAXDatabase",              // z.B. "AX2012R3"
    "UseWindowsAuthentication": "true",   // Windows Auth aktivieren
    "User": "",                           // Leer lassen bei Windows Auth
    "Password": ""
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=IhrAXSQLServer;Database=IhrAXDatabase;Integrated Security=true;TrustServerCertificate=true"
  },
  "AOS": {
    "Servers": ["it-test-erp6", "it-test-erp3"],
    "DefaultServer": "it-test-erp6"
  }
}
```

## 2. Was wird wo konfiguriert?

### AX-Datenbank (für Batch Jobs, Sessions lesen):
- **Konfiguration:** `Database` Sektion
- **Verwendung:** Batch Jobs, Sessions, Blocking Chains aus AX lesen
- **Connection String:** `ConnectionStrings:DefaultConnection`

### Monitoring-Datenbank (für Historie):
- **Konfiguration:** `MonitoringDatabase` Sektion
- **Verwendung:** Historie, Alerts, Baselines speichern
- **Standard:** SQLite (`axmonitoring.db`)

## Schnellstart für Ihre Umgebung

```json
{
  "Database": {
    "Server": "IhrSQLServer",
    "Name": "AX2012R3",
    "UseWindowsAuthentication": "true"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=IhrSQLServer;Database=AX2012R3;Integrated Security=true;TrustServerCertificate=true"
  },
  "AOS": {
    "Servers": ["it-test-erp6", "it-test-erp3"],
    "DefaultServer": "it-test-erp6"
  }
}
```

## Production (Environment Variables)

```powershell
$env:AX_DB_SERVER="SQLSERVER01"
$env:AX_DB_NAME="AX2012R3"
$env:AX_USE_WINDOWS_AUTH="true"
$env:AOS_SERVER_1="it-test-erp6"
$env:AOS_SERVER_2="it-test-erp3"
```

Die `appsettings.json` verwendet dann automatisch diese Platzhalter.

