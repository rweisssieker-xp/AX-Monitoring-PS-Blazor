<#
.SYNOPSIS
    AI/OpenAI integration module for AX Monitor
.DESCRIPTION
    Provides AI-powered features including anomaly detection, predictive analysis,
    chat assistant, and intelligent recommendations
#>

# Import required modules
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Config') -Force
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Database') -Force
Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Monitoring') -Force

#region OpenAI API

function Invoke-OpenAIRequest {
    <#
    .SYNOPSIS
        Make a request to OpenAI API
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Prompt,
        
        [Parameter()]
        [string]$SystemPrompt = "You are an expert AX 2012 R3 and SQL Server performance analyst.",
        
        [Parameter()]
        [int]$MaxTokens,
        
        [Parameter()]
        [double]$Temperature
    )
    
    if (-not $Config.AI.Enabled) {
        throw "AI features are not enabled. Set OPENAI_ENABLED=true and provide OPENAI_API_KEY"
    }
    
    $apiKey = $Config.AI.APIKey
    $model = $Config.AI.Model
    $maxTokens = if ($MaxTokens) { $MaxTokens } else { $Config.AI.MaxTokens }
    $temperature = if ($Temperature) { $Temperature } else { $Config.AI.Temperature }
    
    $headers = @{
        'Authorization' = "Bearer $apiKey"
        'Content-Type' = 'application/json'
    }
    
    $body = @{
        model = $model
        messages = @(
            @{
                role = 'system'
                content = $SystemPrompt
            }
            @{
                role = 'user'
                content = $Prompt
            }
        )
        max_tokens = $maxTokens
        temperature = $temperature
    } | ConvertTo-Json -Depth 10
    
    try {
        $response = Invoke-RestMethod -Uri 'https://api.openai.com/v1/chat/completions' `
            -Method Post `
            -Headers $headers `
            -Body $body
        
        return @{
            Content = $response.choices[0].message.content
            TokensUsed = $response.usage.total_tokens
            Model = $response.model
        }
    }
    catch {
        Write-Error "OpenAI API request failed: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region AI Chat Assistant

function Invoke-AXAIChat {
    <#
    .SYNOPSIS
        AI chat assistant for AX monitoring
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter(Mandatory)]
        [string]$Message,
        
        [Parameter()]
        [hashtable]$Context = @{}
    )
    
    if (-not $Config.AI.Features.ChatAssistant) {
        throw "AI Chat Assistant is not enabled"
    }
    
    # Build context-aware prompt
    $systemPrompt = @"
You are an expert AI assistant for Microsoft Dynamics AX 2012 R3 performance monitoring.
You help operations teams understand system metrics, diagnose issues, and recommend solutions.

Current System Context:
- Environment: $($Config.Environment)
- AX Database: $($Config.AXDatabase.Database)
- Server: $($Config.AXDatabase.Server)

Your responses should be:
1. Concise and actionable
2. Based on AX 2012 R3 and SQL Server best practices
3. Include specific recommendations when possible
4. Reference relevant metrics and thresholds
"@
    
    # Add current metrics to context if available
    if ($Context.Count -eq 0) {
        try {
            $kpiData = Get-AXKPIData -Config $Config
            $Context = @{
                CPUUsage = $kpiData.CPUUsage
                MemoryUsage = $kpiData.MemoryUsage
                ActiveSessions = $kpiData.ActiveSessions
                BatchBacklog = $kpiData.BatchBacklog
                BlockingChains = $kpiData.BlockingChains
            }
        }
        catch {
            Write-Warning "Could not fetch current metrics for context"
        }
    }
    
    $contextString = if ($Context.Count -gt 0) {
        "`n`nCurrent Metrics:`n" + ($Context.GetEnumerator() | ForEach-Object { "- $($_.Key): $($_.Value)" } | Out-String)
    } else { "" }
    
    $fullPrompt = $Message + $contextString
    
    try {
        $response = Invoke-OpenAIRequest -Config $Config -Prompt $fullPrompt -SystemPrompt $systemPrompt
        
        # Save to database
        Save-AIAnalysis -Config $Config -AnalysisType 'Chat' -InputData $Message -Output $response.Content -TokensUsed $response.TokensUsed -Model $response.Model
        
        return $response.Content
    }
    catch {
        Write-Error "AI Chat failed: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Anomaly Detection

function Get-AXAIAnomalies {
    <#
    .SYNOPSIS
        Detect anomalies using AI analysis
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [int]$LastHours = 24
    )
    
    if (-not $Config.AI.Features.AnomalyDetection) {
        throw "AI Anomaly Detection is not enabled"
    }
    
    try {
        # Get historical metrics
        $metricsQuery = @"
SELECT 
    MetricType,
    MetricName,
    AVG(Value) as AvgValue,
    MIN(Value) as MinValue,
    MAX(Value) as MaxValue,
    STDEV(Value) as StdDev,
    COUNT(*) as DataPoints
FROM Metrics
WHERE Timestamp >= DATEADD(HOUR, -$LastHours, GETDATE())
GROUP BY MetricType, MetricName
"@
        
        $historicalMetrics = Invoke-StagingQuery -Config $Config -Query $metricsQuery
        
        # Get current metrics
        $kpiData = Get-AXKPIData -Config $Config
        
        $anomalies = @()
        
        # Analyze CPU
        $cpuMetric = $historicalMetrics | Where-Object { $_.MetricName -eq 'CPUUsage' }
        if ($cpuMetric -and $cpuMetric.StdDev -gt 0) {
            $zScore = ($kpiData.CPUUsage - $cpuMetric.AvgValue) / $cpuMetric.StdDev
            if ([Math]::Abs($zScore) -gt 2) {
                $anomalies += [PSCustomObject]@{
                    MetricName = 'CPU Usage'
                    CurrentValue = $kpiData.CPUUsage
                    ExpectedRange = "$([Math]::Round($cpuMetric.AvgValue - 2*$cpuMetric.StdDev, 2)) - $([Math]::Round($cpuMetric.AvgValue + 2*$cpuMetric.StdDev, 2))"
                    Severity = if ([Math]::Abs($zScore) -gt 3) { 'Critical' } else { 'Warning' }
                    ZScore = [Math]::Round($zScore, 2)
                }
            }
        }
        
        # Analyze Memory
        $memMetric = $historicalMetrics | Where-Object { $_.MetricName -eq 'MemoryUsage' }
        if ($memMetric -and $memMetric.StdDev -gt 0) {
            $zScore = ($kpiData.MemoryUsage - $memMetric.AvgValue) / $memMetric.StdDev
            if ([Math]::Abs($zScore) -gt 2) {
                $anomalies += [PSCustomObject]@{
                    MetricName = 'Memory Usage'
                    CurrentValue = $kpiData.MemoryUsage
                    ExpectedRange = "$([Math]::Round($memMetric.AvgValue - 2*$memMetric.StdDev, 2)) - $([Math]::Round($memMetric.AvgValue + 2*$memMetric.StdDev, 2))"
                    Severity = if ([Math]::Abs($zScore) -gt 3) { 'Critical' } else { 'Warning' }
                    ZScore = [Math]::Round($zScore, 2)
                }
            }
        }
        
        # Use AI for deeper analysis if anomalies found
        if ($anomalies.Count -gt 0) {
            $anomalyPrompt = @"
Analyze these detected anomalies in AX 2012 R3 system:

$($anomalies | ForEach-Object { "- $($_.MetricName): Current=$($_.CurrentValue), Expected=$($_.ExpectedRange), Z-Score=$($_.ZScore)" } | Out-String)

Provide:
1. Root cause analysis
2. Impact assessment
3. Recommended actions
4. Preventive measures

Keep response concise and actionable.
"@
            
            $aiAnalysis = Invoke-OpenAIRequest -Config $Config -Prompt $anomalyPrompt
            
            foreach ($anomaly in $anomalies) {
                $anomaly | Add-Member -NotePropertyName 'AIAnalysis' -NotePropertyValue $aiAnalysis.Content
            }
            
            Save-AIAnalysis -Config $Config -AnalysisType 'AnomalyDetection' -InputData ($anomalies | ConvertTo-Json) -Output $aiAnalysis.Content -TokensUsed $aiAnalysis.TokensUsed -Model $aiAnalysis.Model
        }
        
        return $anomalies
    }
    catch {
        Write-Error "Anomaly detection failed: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Predictive Analysis

function Get-AXAIPredictions {
    <#
    .SYNOPSIS
        Generate predictive insights using AI
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config,
        
        [Parameter()]
        [int]$ForecastHours = 24
    )
    
    if (-not $Config.AI.Features.PredictiveAnalysis) {
        throw "AI Predictive Analysis is not enabled"
    }
    
    try {
        # Get historical trend data
        $trendQuery = @"
SELECT TOP 100
    Timestamp,
    MetricName,
    Value
FROM Metrics
WHERE MetricType = 'KPI'
    AND Timestamp >= DATEADD(HOUR, -168, GETDATE())
ORDER BY Timestamp DESC
"@
        
        $trendData = Invoke-StagingQuery -Config $Config -Query $trendQuery
        
        # Group by metric
        $metricGroups = $trendData | Group-Object -Property MetricName
        
        $predictions = @()
        
        foreach ($group in $metricGroups) {
            $metricName = $group.Name
            $values = $group.Group | Sort-Object -Property Timestamp | Select-Object -ExpandProperty Value
            
            # Simple trend analysis
            $recentAvg = ($values | Select-Object -Last 10 | Measure-Object -Average).Average
            $olderAvg = ($values | Select-Object -First 10 | Measure-Object -Average).Average
            $trend = if ($recentAvg -gt $olderAvg) { 'Increasing' } elseif ($recentAvg -lt $olderAvg) { 'Decreasing' } else { 'Stable' }
            
            $predictions += [PSCustomObject]@{
                MetricName = $metricName
                CurrentAverage = [Math]::Round($recentAvg, 2)
                Trend = $trend
                ChangePercent = if ($olderAvg -gt 0) { [Math]::Round((($recentAvg - $olderAvg) / $olderAvg) * 100, 2) } else { 0 }
            }
        }
        
        # Get AI predictions
        $predictionPrompt = @"
Based on the following metric trends over the past week, predict potential issues in the next $ForecastHours hours:

$($predictions | ForEach-Object { "- $($_.MetricName): Current Avg=$($_.CurrentAverage), Trend=$($_.Trend), Change=$($_.ChangePercent)%" } | Out-String)

Provide:
1. Predicted issues or bottlenecks
2. Estimated timeframe
3. Proactive recommendations
4. Resource requirements

Keep response concise and actionable.
"@
        
        $aiPrediction = Invoke-OpenAIRequest -Config $Config -Prompt $predictionPrompt
        
        Save-AIAnalysis -Config $Config -AnalysisType 'Prediction' -InputData ($predictions | ConvertTo-Json) -Output $aiPrediction.Content -TokensUsed $aiPrediction.TokensUsed -Model $aiPrediction.Model
        
        return @{
            Predictions = $predictions
            AIAnalysis = $aiPrediction.Content
            ForecastHours = $ForecastHours
            GeneratedAt = Get-Date -Format 'o'
        }
    }
    catch {
        Write-Error "Predictive analysis failed: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Recommendations

function Get-AXAIRecommendations {
    <#
    .SYNOPSIS
        Get AI-powered recommendations for system optimization
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        # Gather comprehensive system state
        $kpiData = Get-AXKPIData -Config $Config
        $batchStats = Get-AXBatchJobStatistics -Config $Config
        $sessionStats = Get-AXSessionStatistics -Config $Config
        $sqlHealth = Get-SQLHealthMetrics -Config $Config
        
        $systemState = @"
Current AX 2012 R3 System State:

Performance Metrics:
- CPU Usage: $($kpiData.CPUUsage)%
- Memory Usage: $($kpiData.MemoryUsage)%
- Active Connections: $($kpiData.ActiveConnections)

Batch Jobs:
- Total Jobs (24h): $($batchStats.TotalJobs)
- Running: $($batchStats.RunningJobs)
- Waiting: $($batchStats.WaitingJobs)
- Error Rate: $($batchStats.ErrorRate)%
- Avg Duration: $($batchStats.AvgDurationMinutes) minutes

Sessions:
- Total Sessions: $($sessionStats.TotalSessions)
- Active: $($sessionStats.ActiveSessions)
- Idle: $($sessionStats.IdleSessions)
- Unique Users: $($sessionStats.UniqueUsers)

Blocking:
- Active Blocking Chains: $($kpiData.BlockingChains)

SQL Health:
- Longest Query: $($sqlHealth.LongestQueryMinutes) minutes
"@
        
        $recommendationPrompt = @"
As an AX 2012 R3 performance expert, analyze this system state and provide optimization recommendations:

$systemState

Provide:
1. Top 3 immediate actions (quick wins)
2. Medium-term optimizations (1-2 weeks)
3. Long-term improvements (strategic)
4. Resource allocation recommendations
5. Monitoring enhancements

Format as actionable bullet points with priority levels.
"@
        
        $aiRecommendations = Invoke-OpenAIRequest -Config $Config -Prompt $recommendationPrompt -MaxTokens 1500
        
        Save-AIAnalysis -Config $Config -AnalysisType 'Recommendations' -InputData $systemState -Output $aiRecommendations.Content -TokensUsed $aiRecommendations.TokensUsed -Model $aiRecommendations.Model
        
        return @{
            Recommendations = $aiRecommendations.Content
            SystemState = @{
                KPI = $kpiData
                BatchStats = $batchStats
                SessionStats = $sessionStats
            }
            GeneratedAt = Get-Date -Format 'o'
        }
    }
    catch {
        Write-Error "Failed to generate recommendations: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Background AI Analysis

function Invoke-AXAIAnalysis {
    <#
    .SYNOPSIS
        Run comprehensive AI analysis (scheduled task)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )
    
    try {
        Write-Verbose "Starting AI analysis..."
        
        # Run anomaly detection
        if ($Config.AI.Features.AnomalyDetection) {
            try {
                $anomalies = Get-AXAIAnomalies -Config $Config
                if ($anomalies.Count -gt 0) {
                    Write-Host "  Detected $($anomalies.Count) anomalies" -ForegroundColor Yellow
                    
                    # Create alerts for critical anomalies
                    Import-Module (Join-Path $PSScriptRoot '..\AXMonitor.Alerts') -Force
                    foreach ($anomaly in $anomalies | Where-Object { $_.Severity -eq 'Critical' }) {
                        New-AXAlert -Config $Config `
                            -AlertType 'AIAnomaly' `
                            -Severity 'Warning' `
                            -Message "AI detected anomaly in $($anomaly.MetricName): $($anomaly.CurrentValue)" `
                            -Details $anomaly.AIAnalysis
                    }
                }
            }
            catch {
                Write-Warning "Anomaly detection failed: $($_.Exception.Message)"
            }
        }
        
        # Run predictive analysis (less frequently)
        if ($Config.AI.Features.PredictiveAnalysis) {
            $lastPrediction = Get-LastAIAnalysis -Config $Config -AnalysisType 'Prediction'
            $hoursSinceLastPrediction = if ($lastPrediction) {
                ((Get-Date) - $lastPrediction.CreatedAt).TotalHours
            } else { 999 }
            
            if ($hoursSinceLastPrediction -gt 6) {
                try {
                    $predictionResult = Get-AXAIPredictions -Config $Config
                    Write-Host "  Generated predictions for next 24 hours" -ForegroundColor Cyan
                    Write-Verbose "Predictions: $($predictionResult.Predictions.Count) metrics analyzed"
                }
                catch {
                    Write-Warning "Predictive analysis failed: $($_.Exception.Message)"
                }
            }
        }
        
        Write-Verbose "AI analysis completed"
    }
    catch {
        Write-Error "AI analysis failed: $($_.Exception.Message)"
        throw
    }
}

#endregion

#region Helper Functions

function Save-AIAnalysis {
    <#
    .SYNOPSIS
        Save AI analysis to database
    #>
    param(
        [hashtable]$Config,
        [string]$AnalysisType,
        [string]$InputData,
        [string]$Output,
        [int]$TokensUsed,
        [string]$Model
    )
    
    $query = @"
INSERT INTO AIAnalysis (AnalysisType, Input, Output, TokensUsed, Model)
VALUES (?, ?, ?, ?, ?)
"@
    
    $params = @{
        '@p1' = $AnalysisType
        '@p2' = $InputData
        '@p3' = $Output
        '@p4' = $TokensUsed
        '@p5' = $Model
    }
    
    Invoke-StagingQuery -Config $Config -Query $query -Parameters $params -NonQuery
}

function Get-LastAIAnalysis {
    param(
        [hashtable]$Config,
        [string]$AnalysisType
    )
    
    $query = @"
SELECT TOP 1 *
FROM AIAnalysis
WHERE AnalysisType = ?
ORDER BY CreatedAt DESC
"@
    
    $params = @{ '@p1' = $AnalysisType }
    $result = Invoke-StagingQuery -Config $Config -Query $query -Parameters $params
    
    return if ($result) { $result[0] } else { $null }
}

#endregion

# Export module members
Export-ModuleMember -Function @(
    'Invoke-OpenAIRequest'
    'Invoke-AXAIChat'
    'Get-AXAIAnomalies'
    'Get-AXAIPredictions'
    'Get-AXAIRecommendations'
    'Invoke-AXAIAnalysis'
)
