<#
.SYNOPSIS
    Monitoring module for AX 2012 R3
.DESCRIPTION
    Provides monitoring functions for batch jobs, sessions, blocking, and SQL health
#>

# Import required modules
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Config') -Force
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Database') -Force

#region Batch Job Monitoring

function Get-AXBatchJobs {
    <#
    .SYNOPSIS
        Get current batch jobs from AX database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [ValidateSet('All', 'Running', 'Waiting', 'Error', 'Completed')]
        [string]$Status = 'All'
    )
    
    $query = @"
SELECT 
    BATCHJOBID as Id,
    CAPTION as Name,
    STATUS as StatusCode,
    STARTDATETIME as StartTime,
    ENDDATETIME as EndTime,
    AOSID as AOSServer,
    PROGRESS as Progress,
    COMPANY as Company,
    CREATEDBY as CreatedBy
FROM BATCHJOB 
WHERE 1=1
"@
    
    # Add status filter
    if ($Status -ne 'All') {
        $statusCode = switch ($Status) {
            'Waiting' { 0 }
            'Ready' { 1 }
            'Running' { 2 }
            'Error' { 4 }
            'Completed' { 5 }
        }
        $query += " AND STATUS = $statusCode"
    }
    
    $query += " ORDER BY STARTDATETIME DESC"
    
    try {
        $results = Invoke-AXQuery -Config $Config -Query $query
        
        $batchJobs = foreach ($row in $results) {
            $startTime = if ($row.StartTime) { [DateTime]$row.StartTime } else { $null }
            $endTime = if ($row.EndTime) { [DateTime]$row.EndTime } else { $null }
            
            $duration = if ($startTime -and $endTime) {
                ($endTime - $startTime).TotalMinutes
            }
            elseif ($startTime) {
                ((Get-Date) - $startTime).TotalMinutes
            }
            else {
                0
            }
            
            [PSCustomObject]@{
                Id = $row.Id
                Name = $row.Name
                Status = Get-BatchStatusName -StatusCode $row.StatusCode
                StartTime = $startTime
                EndTime = $endTime
                DurationMinutes = [Math]::Round($duration, 2)
                AOSServer = $row.AOSServer
                Progress = $row.Progress
                Company = $row.Company
                CreatedBy = $row.CreatedBy
            }
        }
        
        return $batchJobs
    }
    catch {
        Write-Error "Failed to get batch jobs: $($_.Exception.Message)"
        throw
    }
}

function Get-BatchStatusName {
    param([int]$StatusCode)
    
    switch ($StatusCode) {
        0 { 'Waiting' }
        1 { 'Ready' }
        2 { 'Running' }
        3 { 'Canceled' }
        4 { 'Error' }
        5 { 'Completed' }
        6 { 'Withhold' }
        7 { 'WithholdCanceled' }
        default { 'Unknown' }
    }
}

function Get-AXBatchJobStatistics {
    <#
    .SYNOPSIS
        Get batch job statistics
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [int]$LastHours = 24
    )
    
    $query = @"
SELECT 
    COUNT(*) as TotalJobs,
    SUM(CASE WHEN STATUS = 2 THEN 1 ELSE 0 END) as RunningJobs,
    SUM(CASE WHEN STATUS = 0 OR STATUS = 1 THEN 1 ELSE 0 END) as WaitingJobs,
    SUM(CASE WHEN STATUS = 4 THEN 1 ELSE 0 END) as ErrorJobs,
    SUM(CASE WHEN STATUS = 5 THEN 1 ELSE 0 END) as CompletedJobs,
    AVG(CASE WHEN STATUS = 5 AND STARTDATETIME IS NOT NULL AND ENDDATETIME IS NOT NULL 
        THEN DATEDIFF(MINUTE, STARTDATETIME, ENDDATETIME) ELSE NULL END) as AvgDurationMinutes
FROM BATCHJOB
WHERE STARTDATETIME >= DATEADD(HOUR, -$LastHours, GETDATE())
"@
    
    try {
        $result = Invoke-AXQuery -Config $Config -Query $query
        
        if ($result) {
            return [PSCustomObject]@{
                TotalJobs = $result[0].TotalJobs
                RunningJobs = $result[0].RunningJobs
                WaitingJobs = $result[0].WaitingJobs
                ErrorJobs = $result[0].ErrorJobs
                CompletedJobs = $result[0].CompletedJobs
                AvgDurationMinutes = [Math]::Round($result[0].AvgDurationMinutes, 2)
                ErrorRate = if ($result[0].TotalJobs -gt 0) { 
                    [Math]::Round(($result[0].ErrorJobs / $result[0].TotalJobs) * 100, 2) 
                } else { 0 }
            }
        }
    }
    catch {
        Write-Error "Failed to get batch statistics: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Session Monitoring

function Get-AXSessions {
    <#
    .SYNOPSIS
        Get current active sessions from AX database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [switch]$ActiveOnly
    )
    
    $query = @"
SELECT 
    SESSIONID as SessionId,
    USERID as UserId,
    AOSID as AOSServer,
    LOGINDATETIME as LoginTime,
    LASTACTIVITYDATETIME as LastActivity,
    STATUS as StatusCode,
    CLIENTCOMPUTER as ClientComputer,
    CLIENTTYPE as ClientType
FROM SYSUSERLOG 
WHERE STATUS = 1
ORDER BY LOGINDATETIME DESC
"@
    
    try {
        $results = Invoke-AXQuery -Config $Config -Query $query
        
        $sessions = foreach ($row in $results) {
            $loginTime = if ($row.LoginTime) { [DateTime]$row.LoginTime } else { $null }
            $lastActivity = if ($row.LastActivity) { [DateTime]$row.LastActivity } else { $loginTime }
            
            $idleMinutes = if ($lastActivity) {
                ((Get-Date) - $lastActivity).TotalMinutes
            } else { 0 }
            
            $sessionStatus = if ($idleMinutes -lt 15) { 'Active' } 
                           elseif ($idleMinutes -lt 60) { 'Idle' }
                           else { 'Inactive' }
            
            if ($ActiveOnly -and $sessionStatus -ne 'Active') {
                continue
            }
            
            [PSCustomObject]@{
                SessionId = $row.SessionId
                UserId = $row.UserId
                AOSServer = $row.AOSServer
                LoginTime = $loginTime
                LastActivity = $lastActivity
                IdleMinutes = [Math]::Round($idleMinutes, 2)
                Status = $sessionStatus
                ClientComputer = $row.ClientComputer
                ClientType = $row.ClientType
            }
        }
        
        return $sessions
    }
    catch {
        Write-Error "Failed to get sessions: $($_.Exception.Message)"
        throw
    }
}

function Get-AXSessionStatistics {
    <#
    .SYNOPSIS
        Get session statistics
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $sessions = Get-AXSessions -Config $Config
    
    $stats = @{
        TotalSessions = $sessions.Count
        ActiveSessions = ($sessions | Where-Object { $_.Status -eq 'Active' }).Count
        IdleSessions = ($sessions | Where-Object { $_.Status -eq 'Idle' }).Count
        InactiveSessions = ($sessions | Where-Object { $_.Status -eq 'Inactive' }).Count
        UniqueUsers = ($sessions | Select-Object -ExpandProperty UserId -Unique).Count
        SessionsByAOS = @{}
    }
    
    # Group by AOS
    $aosSessions = $sessions | Group-Object -Property AOSServer
    foreach ($group in $aosSessions) {
        $stats.SessionsByAOS[$group.Name] = $group.Count
    }
    
    return [PSCustomObject]$stats
}

#endregion

#region Blocking Analysis

function Get-AXBlockingChains {
    <#
    .SYNOPSIS
        Get current SQL blocking chains
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $query = @"
SELECT 
    blocking.session_id as BlockingSession,
    blocked.session_id as BlockedSession,
    blocked.wait_type as WaitType,
    blocked.wait_resource as Resource,
    blocked.wait_time / 1000 as DurationSeconds,
    blocked.command as Command,
    blocked.status as Status,
    DB_NAME(blocked.database_id) as DatabaseName
FROM sys.dm_exec_requests blocked
INNER JOIN sys.dm_exec_requests blocking 
    ON blocked.blocking_session_id = blocking.session_id
WHERE blocked.blocking_session_id > 0
"@
    
    try {
        $results = Invoke-AXQuery -Config $Config -Query $query
        
        $blockingChains = foreach ($row in $results) {
            # Try to get SQL text
            $sqlText = Get-BlockingSQLText -Config $Config -SessionId $row.BlockedSession
            
            [PSCustomObject]@{
                BlockingSession = $row.BlockingSession
                BlockedSession = $row.BlockedSession
                WaitType = $row.WaitType
                Resource = $row.Resource
                DurationSeconds = $row.DurationSeconds
                Command = $row.Command
                Status = $row.Status
                DatabaseName = $row.DatabaseName
                SQLText = $sqlText
            }
        }
        
        return $blockingChains
    }
    catch {
        Write-Error "Failed to get blocking chains: $($_.Exception.Message)"
        throw
    }
}

function Get-BlockingSQLText {
    param(
        [hashtable]$Config,
        [int]$SessionId
    )
    
    $query = @"
SELECT 
    text 
FROM sys.dm_exec_connections c
CROSS APPLY sys.dm_exec_sql_text(c.most_recent_sql_handle) t
WHERE c.session_id = $SessionId
"@
    
    try {
        $result = Invoke-AXQuery -Config $Config -Query $query
        if ($result -and $result[0].text) {
            return $result[0].text.Substring(0, [Math]::Min(500, $result[0].text.Length))
        }
        return 'N/A'
    }
    catch {
        return 'Unable to retrieve'
    }
}

#endregion

#region SQL Health Monitoring

function Get-SQLHealthMetrics {
    <#
    .SYNOPSIS
        Get SQL Server health metrics
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $metrics = @{}
    
    # CPU Usage
    try {
        $cpuQuery = @"
SELECT TOP 1 
    100 - record.value('(./Record/SchedulerMonitorEvent/SystemHealth/SystemIdle)[1]', 'int') as CPUUsage
FROM (
    SELECT CAST(record as xml) as record
    FROM sys.dm_os_ring_buffers
    WHERE ring_buffer_type = N'RING_BUFFER_SCHEDULER_MONITOR'
    AND record LIKE '%<SystemHealth>%'
) as x
ORDER BY record.value('(./Record/@id)[1]', 'int') DESC
"@
        $result = Invoke-AXQuery -Config $Config -Query $cpuQuery
        $metrics['CPUUsage'] = if ($result) { $result[0].CPUUsage } else { 0 }
    }
    catch {
        $metrics['CPUUsage'] = 0
    }
    
    # Memory Usage
    try {
        $memQuery = @"
SELECT 
    (total_physical_memory_kb - available_physical_memory_kb) * 100.0 / total_physical_memory_kb as MemoryUsage
FROM sys.dm_os_sys_memory
"@
        $result = Invoke-AXQuery -Config $Config -Query $memQuery
        $metrics['MemoryUsage'] = if ($result) { [Math]::Round($result[0].MemoryUsage, 2) } else { 0 }
    }
    catch {
        $metrics['MemoryUsage'] = 0
    }
    
    # Active Connections
    try {
        $connQuery = @"
SELECT COUNT(*) as ActiveConnections
FROM sys.dm_exec_sessions 
WHERE is_user_process = 1
"@
        $result = Invoke-AXQuery -Config $Config -Query $connQuery
        $metrics['ActiveConnections'] = if ($result) { $result[0].ActiveConnections } else { 0 }
    }
    catch {
        $metrics['ActiveConnections'] = 0
    }
    
    # Longest Running Query
    try {
        $queryQuery = @"
SELECT 
    ISNULL(MAX(DATEDIFF(MINUTE, start_time, GETDATE())), 0) as LongestQueryMinutes
FROM sys.dm_exec_requests 
WHERE status IN ('running', 'runnable', 'suspended')
"@
        $result = Invoke-AXQuery -Config $Config -Query $queryQuery
        $metrics['LongestQueryMinutes'] = if ($result) { $result[0].LongestQueryMinutes } else { 0 }
    }
    catch {
        $metrics['LongestQueryMinutes'] = 0
    }
    
    # Wait Statistics
    try {
        $waitQuery = @"
SELECT TOP 5
    wait_type,
    wait_time_ms / 1000.0 as WaitTimeSeconds,
    waiting_tasks_count as WaitingTasks
FROM sys.dm_os_wait_stats
WHERE wait_type NOT LIKE 'SLEEP%'
    AND wait_type NOT LIKE 'CLR%'
    AND wait_type NOT LIKE 'BROKER%'
ORDER BY wait_time_ms DESC
"@
        $result = Invoke-AXQuery -Config $Config -Query $waitQuery
        $metrics['TopWaits'] = $result
    }
    catch {
        $metrics['TopWaits'] = @()
    }
    
    return [PSCustomObject]$metrics
}

function Get-SQLDatabaseSize {
    <#
    .SYNOPSIS
        Get database size information
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $query = @"
SELECT 
    DB_NAME() as DatabaseName,
    SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8192.0 / 1024 / 1024) AS UsedSpaceMB,
    SUM(CAST(size AS bigint) * 8192.0 / 1024 / 1024) AS TotalSpaceMB
FROM sys.database_files
GROUP BY name
"@
    
    try {
        $results = Invoke-AXQuery -Config $Config -Query $query
        
        $databases = foreach ($row in $results) {
            [PSCustomObject]@{
                DatabaseName = $row.DatabaseName
                UsedSpaceMB = [Math]::Round($row.UsedSpaceMB, 2)
                TotalSpaceMB = [Math]::Round($row.TotalSpaceMB, 2)
                FreeSpaceMB = [Math]::Round($row.TotalSpaceMB - $row.UsedSpaceMB, 2)
                UsagePercent = [Math]::Round(($row.UsedSpaceMB / $row.TotalSpaceMB) * 100, 2)
            }
        }
        
        return $databases
    }
    catch {
        Write-Error "Failed to get database size: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region KPI and Dashboard

function Get-AXKPIData {
    <#
    .SYNOPSIS
        Get KPI data for dashboard
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        $batchStats = Get-AXBatchJobStatistics -Config $Config
        $sessionStats = Get-AXSessionStatistics -Config $Config
        $blockingChains = Get-AXBlockingChains -Config $Config
        $sqlHealth = Get-SQLHealthMetrics -Config $Config
        
        return [PSCustomObject]@{
            BatchBacklog = $batchStats.WaitingJobs + $batchStats.RunningJobs
            ErrorRate = $batchStats.ErrorRate
            ActiveSessions = $sessionStats.ActiveSessions
            BlockingChains = $blockingChains.Count
            CPUUsage = $sqlHealth.CPUUsage
            MemoryUsage = $sqlHealth.MemoryUsage
            ActiveConnections = $sqlHealth.ActiveConnections
            Timestamp = Get-Date -Format 'o'
        }
    }
    catch {
        Write-Error "Failed to get KPI data: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Data Collection

function Invoke-AXMetricsCollection {
    <#
    .SYNOPSIS
        Collect and store metrics to staging database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        $kpiData = Get-AXKPIData -Config $Config
        
        # Save metrics
        Save-MetricToStaging -Config $Config -MetricType 'KPI' -MetricName 'BatchBacklog' -Value $kpiData.BatchBacklog
        Save-MetricToStaging -Config $Config -MetricType 'KPI' -MetricName 'ErrorRate' -Value $kpiData.ErrorRate
        Save-MetricToStaging -Config $Config -MetricType 'KPI' -MetricName 'ActiveSessions' -Value $kpiData.ActiveSessions
        Save-MetricToStaging -Config $Config -MetricType 'KPI' -MetricName 'BlockingChains' -Value $kpiData.BlockingChains
        Save-MetricToStaging -Config $Config -MetricType 'SQL' -MetricName 'CPUUsage' -Value $kpiData.CPUUsage
        Save-MetricToStaging -Config $Config -MetricType 'SQL' -MetricName 'MemoryUsage' -Value $kpiData.MemoryUsage
        
        Write-Verbose "Metrics collected successfully"
    }
    catch {
        Write-Error "Metrics collection failed: $($_.Exception.Message)"
        throw
    }
}

function Invoke-AXDataCleanup {
    <#
    .SYNOPSIS
        Cleanup old data from staging database
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    $retentionDays = $Config.Monitoring.RetentionDays
    
    $queries = @(
        "DELETE FROM Metrics WHERE CreatedAt < DATEADD(DAY, -$retentionDays, GETDATE())"
        "DELETE FROM BatchJobHistory WHERE CollectedAt < DATEADD(DAY, -$retentionDays, GETDATE())"
        "DELETE FROM SessionHistory WHERE CollectedAt < DATEADD(DAY, -$retentionDays, GETDATE())"
        "DELETE FROM AIAnalysis WHERE CreatedAt < DATEADD(DAY, -$retentionDays, GETDATE())"
    )
    
    foreach ($query in $queries) {
        try {
            Invoke-StagingQuery -Config $Config -Query $query -NonQuery
        }
        catch {
            Write-Warning "Cleanup query failed: $($_.Exception.Message)"
        }
    }
    
    Write-Verbose "Data cleanup completed"
}

#endregion

# Export module members
Export-ModuleMember -Function @(
    'Get-AXBatchJobs'
    'Get-AXBatchJobStatistics'
    'Get-AXSessions'
    'Get-AXSessionStatistics'
    'Get-AXBlockingChains'
    'Get-SQLHealthMetrics'
    'Get-SQLDatabaseSize'
    'Get-AXKPIData'
    'Invoke-AXMetricsCollection'
    'Invoke-AXDataCleanup'
)
