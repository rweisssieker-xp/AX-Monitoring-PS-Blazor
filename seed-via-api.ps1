# Demo-Daten über API einfügen
$apiUrl = "http://localhost:5079/api/v1"

Write-Host "Füge Demo-Daten über API ein..." -ForegroundColor Green

# Test: API erreichbar?
try {
    $testResponse = Invoke-WebRequest -Uri "$apiUrl/health" -Method Get -UseBasicParsing -TimeoutSec 5
    Write-Host "✓ API ist erreichbar" -ForegroundColor Green
} catch {
    Write-Host "✗ API ist nicht erreichbar. Stelle sicher, dass die API läuft." -ForegroundColor Red
    exit 1
}

# Batch Job einfügen (vereinfacht - nur POST wenn Endpoint vorhanden)
$batchJobs = @(
    @{
        batchJobId = "AX7654321"
        name = "CustInvoicePost - Daily"
        status = "Completed"
        aosServer = "bras3333"
        startTime = (Get-Date).AddHours(-2).ToString("o")
        endTime = (Get-Date).AddHours(-1).ToString("o")
        createdAt = (Get-Date).AddHours(-3).ToString("o")
        progress = 100
    },
    @{
        batchJobId = "AX7654322"
        name = "SalesOrderInvoicing"
        status = "Running"
        aosServer = "bras3333"
        startTime = (Get-Date).AddMinutes(-30).ToString("o")
        endTime = $null
        createdAt = (Get-Date).AddMinutes(-35).ToString("o")
        progress = 65
    },
    @{
        batchJobId = "AX7654323"
        name = "PurchInvoicePost"
        status = "Error"
        aosServer = "bras3333"
        startTime = (Get-Date).AddHours(-4).ToString("o")
        endTime = (Get-Date).AddHours(-3).ToString("o")
        createdAt = (Get-Date).AddHours(-5).ToString("o")
        progress = 0
    }
)

Write-Host "`nHinweis: Die meisten POST-Endpunkte sind möglicherweise nicht implementiert." -ForegroundColor Yellow
Write-Host "Die Daten werden über den MonitoringUpdateService automatisch gesammelt," -ForegroundColor Yellow
Write-Host "sobald eine Verbindung zur AX-Datenbank besteht.`n" -ForegroundColor Yellow

# Zeige verfügbare Daten
Write-Host "Aktuelle Daten aus der API:" -ForegroundColor Cyan

try {
    $jobs = Invoke-RestMethod -Uri "$apiUrl/batch-jobs" -Method Get
    Write-Host "Batch Jobs: $($jobs.Count)" -ForegroundColor Gray

    $alerts = Invoke-RestMethod -Uri "$apiUrl/alerts" -Method Get
    Write-Host "Alerts: $($alerts.Count)" -ForegroundColor Gray

    $sessions = Invoke-RestMethod -Uri "$apiUrl/sessions" -Method Get
    Write-Host "Sessions: $($sessions.Count)" -ForegroundColor Gray
} catch {
    Write-Host "Fehler beim Abrufen der Daten: $_" -ForegroundColor Red
}

Write-Host "`nUm echte Daten zu sehen, muss die Verbindung zur AX-Datenbank funktionieren." -ForegroundColor Yellow
