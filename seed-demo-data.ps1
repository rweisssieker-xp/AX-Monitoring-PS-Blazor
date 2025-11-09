# Demo-Daten für AX Monitoring laden
# Dieses Script füllt die SQLite-Datenbank mit Beispieldaten

Write-Host "Generiere Demo-Daten für AX Monitoring..." -ForegroundColor Green

# API Endpoint
$apiUrl = "http://localhost:5079/api/v1"

# Funktion zum Hinzufügen von Batch Jobs
function Add-DemoBatchJobs {
    Write-Host "Füge Batch Jobs hinzu..."

    $statuses = @("Waiting", "Running", "Completed", "Error")
    $jobNames = @(
        "CustInvoicePost",
        "SalesOrderInvoicing",
        "PurchInvoicePost",
        "InventCounting",
        "GLJournalPost",
        "BOMCalc",
        "MasterPlanScheduling",
        "CustCollectionLetter"
    )

    for ($i = 0; $i -lt 50; $i++) {
        $status = $statuses | Get-Random
        $jobName = $jobNames | Get-Random
        $startTime = (Get-Date).AddDays(-([Math]::Floor((Get-Random -Minimum 0 -Maximum 30)))).AddHours(-([Math]::Floor((Get-Random -Minimum 0 -Maximum 24))))

        $duration = Get-Random -Minimum 1 -Maximum 3600
        $endTime = if ($status -eq "Completed" -or $status -eq "Error") {
            $startTime.AddSeconds($duration)
        } else {
            $null
        }

        $batchJob = @{
            batchJobId = "AX" + (Get-Random -Minimum 1000000 -Maximum 9999999)
            name = "$jobName - $i"
            status = $status
            aosServer = "bras3333"
            startTime = $startTime.ToString("o")
            endTime = if ($endTime) { $endTime.ToString("o") } else { $null }
            createdAt = $startTime.AddMinutes(-5).ToString("o")
            progress = if ($status -eq "Running") { Get-Random -Minimum 10 -Maximum 90 } else { 0 }
        } | ConvertTo-Json

        try {
            Invoke-RestMethod -Uri "$apiUrl/batch-jobs" -Method Post -Body $batchJob -ContentType "application/json" -ErrorAction SilentlyContinue | Out-Null
        } catch {
            # Ignore errors
        }
    }
    Write-Host "✓ Batch Jobs hinzugefügt" -ForegroundColor Green
}

# Funktion zum Hinzufügen von Alerts
function Add-DemoAlerts {
    Write-Host "Füge Alerts hinzu..."

    $severities = @("Critical", "Warning", "Info")
    $alertTypes = @("CPU", "Memory", "Disk", "Deadlock", "Blocking", "BatchError")

    for ($i = 0; $i -lt 20; $i++) {
        $severity = $severities | Get-Random
        $alertType = $alertTypes | Get-Random
        $timestamp = (Get-Date).AddHours(-([Math]::Floor((Get-Random -Minimum 0 -Maximum 72))))

        $alert = @{
            severity = $severity
            title = "$alertType Alert on bras3333"
            message = "Demo alert: $alertType threshold exceeded"
            source = "AX_Monitor"
            environment = "DEV"
            timestamp = $timestamp.ToString("o")
            acknowledged = (Get-Random -Minimum 0 -Maximum 10) -lt 3
            acknowledgedBy = if ((Get-Random -Minimum 0 -Maximum 10) -lt 3) { "admin" } else { $null }
        } | ConvertTo-Json

        try {
            Invoke-RestMethod -Uri "$apiUrl/alerts" -Method Post -Body $alert -ContentType "application/json" -ErrorAction SilentlyContinue | Out-Null
        } catch {
            # Ignore errors
        }
    }
    Write-Host "✓ Alerts hinzugefügt" -ForegroundColor Green
}

# Funktion zum Hinzufügen von Sessions
function Add-DemoSessions {
    Write-Host "Füge Sessions hinzu..."

    $userNames = @("admin", "jdoe", "msmith", "bjones", "skumar", "awilson")
    $clientTypes = @("AX Client", "Web", "Service", "Batch")

    for ($i = 0; $i -lt 30; $i++) {
        $userName = $userNames | Get-Random
        $clientType = $clientTypes | Get-Random
        $loginTime = (Get-Date).AddHours(-([Math]::Floor((Get-Random -Minimum 0 -Maximum 48))))
        $isActive = (Get-Random -Minimum 0 -Maximum 10) -lt 7

        $session = @{
            sessionId = "S" + (Get-Random -Minimum 100000 -Maximum 999999)
            userId = $userName
            userName = $userName
            clientType = $clientType
            serverId = "bras3333"
            loginTime = $loginTime.ToString("o")
            lastActivity = (Get-Date).AddMinutes(-([Math]::Floor((Get-Random -Minimum 0 -Maximum 120)))).ToString("o")
            status = if ($isActive) { "Active" } else { "Idle" }
            cpuTime = Get-Random -Minimum 0 -Maximum 10000
            memoryUsage = Get-Random -Minimum 10 -Maximum 500
        } | ConvertTo-Json

        try {
            Invoke-RestMethod -Uri "$apiUrl/sessions" -Method Post -Body $session -ContentType "application/json" -ErrorAction SilentlyContinue | Out-Null
        } catch {
            # Ignore errors
        }
    }
    Write-Host "✓ Sessions hinzugefügt" -ForegroundColor Green
}

# Hauptausführung
Write-Host "`nStarte Demo-Daten-Generierung..." -ForegroundColor Cyan
Write-Host "API: $apiUrl`n" -ForegroundColor Gray

# Warte kurz, bis die API bereit ist
Start-Sleep -Seconds 2

try {
    Add-DemoBatchJobs
    Add-DemoAlerts
    Add-DemoSessions

    Write-Host "`n✓ Demo-Daten erfolgreich generiert!" -ForegroundColor Green
    Write-Host "Öffne die Anwendung unter: http://localhost:5108" -ForegroundColor Cyan
} catch {
    Write-Host "`n✗ Fehler beim Generieren der Demo-Daten: $_" -ForegroundColor Red
    Write-Host "Stelle sicher, dass die API läuft auf: $apiUrl" -ForegroundColor Yellow
}
