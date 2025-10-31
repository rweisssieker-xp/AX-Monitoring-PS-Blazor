<#
.SYNOPSIS
    Alerting module for AX Monitor
.DESCRIPTION
    Provides alerting functionality with email, Teams, and Slack integration
#>

# Import required modules
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Config') -Force
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Database') -Force

#region Alert Management

function Get-AXAlerts {
    <#
    .SYNOPSIS
        Get alerts from staging database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [ValidateSet('All', 'Active', 'Acknowledged', 'Resolved')]
        [string]$Status = 'All',
        
        [Parameter()]
        [int]$LastHours = 24
    )
    
    $query = @"
SELECT 
    Id,
    AlertType,
    Severity,
    Message,
    Details,
    Status,
    AcknowledgedBy,
    AcknowledgedAt,
    ResolvedAt,
    CreatedAt
FROM Alerts
WHERE CreatedAt >= DATEADD(HOUR, -$LastHours, GETDATE())
"@
    
    if ($Status -ne 'All') {
        $query += " AND Status = '$Status'"
    }
    
    $query += " ORDER BY CreatedAt DESC"
    
    try {
        $results = Invoke-StagingQuery -Config $Config -Query $query
        
        $alerts = foreach ($row in $results) {
            [PSCustomObject]@{
                Id = $row.Id
                AlertType = $row.AlertType
                Severity = $row.Severity
                Message = $row.Message
                Details = $row.Details
                Status = $row.Status
                AcknowledgedBy = $row.AcknowledgedBy
                AcknowledgedAt = $row.AcknowledgedAt
                ResolvedAt = $row.ResolvedAt
                CreatedAt = $row.CreatedAt
            }
        }
        
        return $alerts
    }
    catch {
        Write-Error "Failed to get alerts: $($_.Exception.Message)"
        throw
    }
}

function New-AXAlert {
    <#
    .SYNOPSIS
        Create a new alert
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$AlertType,
        
        [Parameter(Mandatory)]
        [ValidateSet('Info', 'Warning', 'Critical')]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message,
        
        [Parameter()]
        [string]$Details
    )
    
    $query = @"
INSERT INTO Alerts (AlertType, Severity, Message, Details, Status)
VALUES (?, ?, ?, ?, 'Active')
SELECT CAST(SCOPE_IDENTITY() as bigint) as Id
"@
    
    try {
        $params = @{
            '@p1' = $AlertType
            '@p2' = $Severity
            '@p3' = $Message
            '@p4' = $Details
        }
        
        $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params
        $alertId = $result[0].Id
        
        # Send notifications
        Send-AlertNotifications -Config $Config -AlertId $alertId -AlertType $AlertType -Severity $Severity -Message $Message
        
        return $alertId
    }
    catch {
        Write-Error "Failed to create alert: $($_.Exception.Message)"
        throw
    }
}

function Set-AXAlertAcknowledged {
    <#
    .SYNOPSIS
        Acknowledge an alert
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [long]$AlertId,
        
        [Parameter()]
        [string]$AcknowledgedBy = $env:USERNAME
    )
    
    $query = @"
UPDATE Alerts
SET Status = 'Acknowledged',
    AcknowledgedBy = ?,
    AcknowledgedAt = GETDATE()
WHERE Id = ?
"@
    
    try {
        $params = @{
            '@p1' = $AcknowledgedBy
            '@p2' = $AlertId
        }
        
        Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
        Write-Verbose "Alert $AlertId acknowledged by $AcknowledgedBy"
    }
    catch {
        Write-Error "Failed to acknowledge alert: $($_.Exception.Message)"
        throw
    }
}

function Set-AXAlertResolved {
    <#
    .SYNOPSIS
        Resolve an alert
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [long]$AlertId
    )
    
    $query = @"
UPDATE Alerts
SET Status = 'Resolved',
    ResolvedAt = GETDATE()
WHERE Id = ?
"@
    
    try {
        $params = @{ '@p1' = $AlertId }
        Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
        Write-Verbose "Alert $AlertId resolved"
    }
    catch {
        Write-Error "Failed to resolve alert: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Alert Rules

function Invoke-AXAlertCheck {
    <#
    .SYNOPSIS
        Check alert rules and create alerts if thresholds are exceeded
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        # Import monitoring module
        Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Monitoring') -Force
        
        # Get current metrics
        $kpiData = Get-AXKPIData -Config $Config
        $thresholds = $Config.Monitoring.Thresholds
        
        # Check CPU threshold
        if ($kpiData.CPUUsage -gt $thresholds.CPUPercent) {
            $existingAlert = Get-RecentAlert -Config $Config -AlertType 'HighCPU' -MinutesAgo 10
            if (-not $existingAlert) {
                New-AXAlert -Config $Config `
                    -AlertType 'HighCPU' `
                    -Severity 'Warning' `
                    -Message "CPU usage is at $($kpiData.CPUUsage)%, exceeding threshold of $($thresholds.CPUPercent)%" `
                    -Details "Current CPU: $($kpiData.CPUUsage)%, Threshold: $($thresholds.CPUPercent)%"
            }
        }
        
        # Check Memory threshold
        if ($kpiData.MemoryUsage -gt $thresholds.MemoryPercent) {
            $existingAlert = Get-RecentAlert -Config $Config -AlertType 'HighMemory' -MinutesAgo 10
            if (-not $existingAlert) {
                New-AXAlert -Config $Config `
                    -AlertType 'HighMemory' `
                    -Severity 'Warning' `
                    -Message "Memory usage is at $($kpiData.MemoryUsage)%, exceeding threshold of $($thresholds.MemoryPercent)%" `
                    -Details "Current Memory: $($kpiData.MemoryUsage)%, Threshold: $($thresholds.MemoryPercent)%"
            }
        }
        
        # Check Blocking
        if ($kpiData.BlockingChains -gt 0) {
            $blockingChains = Get-AXBlockingChains -Config $Config
            foreach ($chain in $blockingChains) {
                if ($chain.DurationSeconds -gt $thresholds.BlockingDurationSeconds) {
                    $existingAlert = Get-RecentAlert -Config $Config -AlertType 'SQLBlocking' -MinutesAgo 5
                    if (-not $existingAlert) {
                        New-AXAlert -Config $Config `
                            -AlertType 'SQLBlocking' `
                            -Severity 'Critical' `
                            -Message "SQL blocking detected for $($chain.DurationSeconds) seconds" `
                            -Details "Blocking Session: $($chain.BlockingSession), Blocked Session: $($chain.BlockedSession), Resource: $($chain.Resource)"
                    }
                }
            }
        }
        
        # Check Batch Job Errors
        $batchStats = Get-AXBatchJobStatistics -Config $Config
        if ($batchStats.ErrorRate -gt 10) {
            $existingAlert = Get-RecentAlert -Config $Config -AlertType 'BatchErrors' -MinutesAgo 15
            if (-not $existingAlert) {
                New-AXAlert -Config $Config `
                    -AlertType 'BatchErrors' `
                    -Severity 'Warning' `
                    -Message "Batch job error rate is $($batchStats.ErrorRate)%" `
                    -Details "Error Jobs: $($batchStats.ErrorJobs), Total Jobs: $($batchStats.TotalJobs)"
            }
        }
        
        Write-Verbose "Alert check completed"
    }
    catch {
        Write-Error "Alert check failed: $($_.Exception.Message)"
        throw
    }
}

function Get-RecentAlert {
    <#
    .SYNOPSIS
        Check if a similar alert was created recently
    #>
    param(
        [hashtable]$Config,
        [string]$AlertType,
        [int]$MinutesAgo
    )
    
    $query = @"
SELECT TOP 1 Id
FROM Alerts
WHERE AlertType = ?
    AND Status = 'Active'
    AND CreatedAt >= DATEADD(MINUTE, -$MinutesAgo, GETDATE())
"@
    
    try {
        $params = @{ '@p1' = $AlertType }
        $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params
        return $result.Count -gt 0
    }
    catch {
        return $false
    }
}

#endregion

#region Notification Services

function Send-AlertNotifications {
    <#
    .SYNOPSIS
        Send alert notifications via configured channels
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [long]$AlertId,
        
        [Parameter(Mandatory)]
        [string]$AlertType,
        
        [Parameter(Mandatory)]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message
    )
    
    # Send email notification
    if ($Config.Alerts.Email.Enabled) {
        try {
            Send-EmailAlert -Config $Config -AlertType $AlertType -Severity $Severity -Message $Message
        }
        catch {
            Write-Warning "Failed to send email alert: $($_.Exception.Message)"
        }
    }
    
    # Send Teams notification
    if ($Config.Alerts.Teams.Enabled) {
        try {
            Send-TeamsAlert -Config $Config -AlertType $AlertType -Severity $Severity -Message $Message
        }
        catch {
            Write-Warning "Failed to send Teams alert: $($_.Exception.Message)"
        }
    }
    
    # Send Slack notification
    if ($Config.Alerts.Slack.Enabled) {
        try {
            Send-SlackAlert -Config $Config -AlertType $AlertType -Severity $Severity -Message $Message
        }
        catch {
            Write-Warning "Failed to send Slack alert: $($_.Exception.Message)"
        }
    }
}

function Send-EmailAlert {
    <#
    .SYNOPSIS
        Send email alert
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$AlertType,
        
        [Parameter(Mandatory)]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message
    )
    
    $emailConfig = $Config.Alerts.Email
    
    $subject = "[$($Config.Environment)] AX Monitor Alert: $AlertType ($Severity)"
    
    $body = @"
<html>
<body style="font-family: Arial, sans-serif;">
    <h2 style="color: $(if ($Severity -eq 'Critical') { '#dc3545' } elseif ($Severity -eq 'Warning') { '#ffc107' } else { '#17a2b8' });">
        AX Monitor Alert
    </h2>
    <table style="border-collapse: collapse; width: 100%;">
        <tr>
            <td style="padding: 8px; border: 1px solid #ddd; font-weight: bold;">Environment:</td>
            <td style="padding: 8px; border: 1px solid #ddd;">$($Config.Environment)</td>
        </tr>
        <tr>
            <td style="padding: 8px; border: 1px solid #ddd; font-weight: bold;">Alert Type:</td>
            <td style="padding: 8px; border: 1px solid #ddd;">$AlertType</td>
        </tr>
        <tr>
            <td style="padding: 8px; border: 1px solid #ddd; font-weight: bold;">Severity:</td>
            <td style="padding: 8px; border: 1px solid #ddd;">$Severity</td>
        </tr>
        <tr>
            <td style="padding: 8px; border: 1px solid #ddd; font-weight: bold;">Message:</td>
            <td style="padding: 8px; border: 1px solid #ddd;">$Message</td>
        </tr>
        <tr>
            <td style="padding: 8px; border: 1px solid #ddd; font-weight: bold;">Timestamp:</td>
            <td style="padding: 8px; border: 1px solid #ddd;">$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</td>
        </tr>
    </table>
    <p style="margin-top: 20px;">
        <a href="http://localhost:8080/alerts" style="background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px;">
            View Dashboard
        </a>
    </p>
</body>
</html>
"@
    
    $mailParams = @{
        SmtpServer = $emailConfig.SMTPServer
        Port = $emailConfig.SMTPPort
        From = $emailConfig.FromAddress
        To = $emailConfig.ToAddresses
        Subject = $subject
        Body = $body
        BodyAsHtml = $true
        UseSsl = $emailConfig.UseSSL
    }
    
    if ($emailConfig.SMTPUser -and $emailConfig.SMTPPassword) {
        $securePassword = ConvertTo-SecureString $emailConfig.SMTPPassword -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential($emailConfig.SMTPUser, $securePassword)
        $mailParams['Credential'] = $credential
    }
    
    Send-MailMessage @mailParams
    Write-Verbose "Email alert sent to $($emailConfig.ToAddresses -join ', ')"
}

function Send-TeamsAlert {
    <#
    .SYNOPSIS
        Send Microsoft Teams alert via webhook
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$AlertType,
        
        [Parameter(Mandatory)]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message
    )
    
    $webhookUrl = $Config.Alerts.Teams.WebhookURL
    
    $color = switch ($Severity) {
        'Critical' { 'FF0000' }
        'Warning' { 'FFA500' }
        'Info' { '0078D4' }
        default { '808080' }
    }
    
    $card = @{
        '@type' = 'MessageCard'
        '@context' = 'https://schema.org/extensions'
        summary = "AX Monitor Alert: $AlertType"
        themeColor = $color
        title = "🚨 AX Monitor Alert"
        sections = @(
            @{
                activityTitle = $AlertType
                activitySubtitle = "Severity: $Severity"
                facts = @(
                    @{ name = 'Environment'; value = $Config.Environment }
                    @{ name = 'Message'; value = $Message }
                    @{ name = 'Timestamp'; value = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss') }
                )
            }
        )
        potentialAction = @(
            @{
                '@type' = 'OpenUri'
                name = 'View Dashboard'
                targets = @(
                    @{ os = 'default'; uri = 'http://localhost:8080/alerts' }
                )
            }
        )
    }
    
    $body = $card | ConvertTo-Json -Depth 10
    
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $body -ContentType 'application/json'
    Write-Verbose "Teams alert sent"
}

function Send-SlackAlert {
    <#
    .SYNOPSIS
        Send Slack alert via webhook
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$AlertType,
        
        [Parameter(Mandatory)]
        [string]$Severity,
        
        [Parameter(Mandatory)]
        [string]$Message
    )
    
    $webhookUrl = $Config.Alerts.Slack.WebhookURL
    
    $emoji = switch ($Severity) {
        'Critical' { ':red_circle:' }
        'Warning' { ':warning:' }
        'Info' { ':information_source:' }
        default { ':grey_question:' }
    }
    
    $payload = @{
        text = "$emoji *AX Monitor Alert*"
        attachments = @(
            @{
                color = switch ($Severity) {
                    'Critical' { 'danger' }
                    'Warning' { 'warning' }
                    'Info' { 'good' }
                    default { '#808080' }
                }
                fields = @(
                    @{ title = 'Environment'; value = $Config.Environment; short = $true }
                    @{ title = 'Alert Type'; value = $AlertType; short = $true }
                    @{ title = 'Severity'; value = $Severity; short = $true }
                    @{ title = 'Message'; value = $Message; short = $false }
                )
                footer = 'AX Monitor'
                ts = [int][double]::Parse((Get-Date -UFormat %s))
            }
        )
    }
    
    $body = $payload | ConvertTo-Json -Depth 10
    
    Invoke-RestMethod -Uri $webhookUrl -Method Post -Body $body -ContentType 'application/json'
    Write-Verbose "Slack alert sent"
}

#endregion

# Export module members
Export-ModuleMember -Function @(
    'Get-AXAlerts'
    'New-AXAlert'
    'Set-AXAlertAcknowledged'
    'Set-AXAlertResolved'
    'Invoke-AXAlertCheck'
    'Send-AlertNotifications'
    'Send-EmailAlert'
    'Send-TeamsAlert'
    'Send-SlackAlert'
)
