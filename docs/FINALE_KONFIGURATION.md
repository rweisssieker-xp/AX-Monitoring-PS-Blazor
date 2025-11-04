# Finale Konfiguration - Übersicht

## Wichtige Konfigurationsdateien

### 1. **Development (lokal testen)**
📁 `AXMonitoringBU.Api/appsettings.Development.json`

**Was Sie hier einstellen:**
- ✅ **AOS Server** (bereits erledigt: `it-test-erp6`, `it-test-erp3`)
- ⚠️ **Datenbank** - Aktuell SQLite, für echte AX-DB ändern:

```json
{
  "Database": {
    "Provider": "SqlServer",
    "Server": "IhrSQLServer",
    "Name": "IhrAXDatabase",
    "UseWindowsAuthentication": "true",  // oder "false" mit User/Password
    "User": "",  // Leer lassen wenn Windows Auth
    "Password": ""
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=IhrSQLServer;Database=IhrAXDatabase;Integrated Security=true;TrustServerCertificate=true"
  }
}
```

### 2. **Production**
📁 `AXMonitoringBU.Api/appsettings.json` (mit Environment Variables)

**Option A: Environment Variables setzen** (empfohlen)
```powershell
# Windows PowerShell
$env:DB_SERVER="SQLSERVER01"
$env:DB_NAME="AX2012R3"
$env:USE_WINDOWS_AUTH="true"
$env:AOS_SERVER_1="it-test-erp6"
$env:AOS_SERVER_2="it-test-erp3"
```

**Option B: Direkt in appsettings.json** (weniger sicher)
Bearbeiten Sie `AXMonitoringBU.Api/appsettings.json` und ersetzen Sie die Platzhalter:
```json
{
  "Database": {
    "Server": "SQLSERVER01",
    "Name": "AX2012R3",
    "UseWindowsAuthentication": "true"
  },
  "AOS": {
    "Servers": ["it-test-erp6", "it-test-erp3"],
    "DefaultServer": "it-test-erp6"
  }
}
```

## Checkliste für finale Konfiguration

### ✅ Bereits konfiguriert:
- [x] AOS Server: `it-test-erp6`, `it-test-erp3`

### ⚠️ Noch zu konfigurieren:

**Für Development:**
- [ ] Datenbank-Server und Datenbankname in `appsettings.Development.json`
- [ ] Windows Authentication aktivieren (`UseWindowsAuthentication: "true"`) ODER SQL Auth mit User/Password

**Für Production:**
- [ ] Environment Variables setzen ODER `appsettings.json` direkt bearbeiten
- [ ] Alle `${PLATZHALTER}` durch echte Werte ersetzen

## Schnellstart - Minimal-Konfiguration

### Development (appsettings.Development.json):

```json
{
  "Database": {
    "Provider": "SqlServer",
    "Server": "IhrSQLServer",
    "Name": "IhrAXDatabase",
    "UseWindowsAuthentication": "true"
  },
  "AOS": {
    "Servers": ["it-test-erp6", "it-test-erp3"],
    "DefaultServer": "it-test-erp6"
  }
}
```

Das ist alles was Sie für den Start benötigen! Die App verwendet dann:
- Windows Authentication für die Datenbank
- Ihre AOS-Server wie konfiguriert
- Automatische Windows-Benutzer-Erkennung für Alerts

## Konfiguration testen

Nach dem Start prüfen:
- **Database Info**: https://localhost:7001/api/v1/database/info
- **Current User**: https://localhost:7001/api/v1/auth/current-user
- **Health**: https://localhost:7001/health

Die konfigurierte Datenbank und AOS-Server werden automatisch im Header angezeigt!

