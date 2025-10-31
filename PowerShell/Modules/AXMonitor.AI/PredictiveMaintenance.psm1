# AXMonitor.AI - Predictive Maintenance Models Module
# Purpose: Provides predictive maintenance models for AX 2012 R3 components
# Author: Qwen Code
# Date: 2025-10-29

# Requires PowerShell 5.1+ or PowerShell 7+

# Exported functions
function Predict-ComponentFailure {
    <#
    .SYNOPSIS
    Predicts component failures based on historical performance and usage data
    
    .DESCRIPTION
    This function uses historical data to predict when AX components might fail
    or require maintenance, enabling proactive intervention.
    
    .PARAMETER ComponentData
    Historical data for a specific component
    
    .PARAMETER ComponentType
    Type of component (e.g., "AOS", "Database", "BatchServer", "ReportServer")
    
    .PARAMETER LookaheadPeriod
    Period to predict failure within (in hours)
    
    .PARAMETER Features
    Features to use for prediction
    
    .EXAMPLE
    $prediction = Predict-ComponentFailure -ComponentData $aostats -ComponentType "AOS" -LookaheadPeriod 48
    
    .NOTES
    This function helps prevent unplanned downtime through predictive maintenance.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$ComponentData,
        
        [Parameter(Mandatory=$true)]
        [ValidateSet("AOS", "Database", "BatchServer", "ReportServer", "FileServer", "Application")]
        [string]$ComponentType,
        
        [Parameter()]
        [int]$LookaheadPeriod = 24,
        
        [Parameter()]
        [string[]]$Features = @("CPU_Usage", "Memory_Usage", "Disk_IO", "Response_Time", "Active_Sessions", "Error_Count")
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Component failure prediction completed successfully"
        FailureProbability = 0.0
        RiskLevel = "Low"
        PredictionWindow = $LookaheadPeriod
        ContributingFactors = @()
        RecommendedActions = @()
        ConfidenceScore = 0.80
    }
    
    try {
        if ($ComponentData.Count -lt 10) {
            $result.Status = "Error"
            $result.Message = "Insufficient data for failure prediction (minimum 10 data points required)"
            $result.ConfidenceScore = 0.3
            return $result
        }
        
        # Calculate various risk indicators from historical data
        $riskFactors = Calculate-RiskIndicators -Data $ComponentData -Features $Features
        
        # Calculate failure probability based on risk factors
        $failureProbability = 0.0
        $factors = @()
        
        # Weight different factors based on component type
        switch ($ComponentType) {
            "AOS" {
                # For AOS servers, high CPU, memory, and session counts are risk factors
                $cpuRisk = if ($riskFactors.AverageCPU -gt 85) { 0.4 } elseif ($riskFactors.AverageCPU -gt 75) { 0.2 } else { 0.05 }
                $memoryRisk = if ($riskFactors.AverageMemory -gt 90) { 0.3 } elseif ($riskFactors.AverageMemory -gt 80) { 0.15 } else { 0.05 }
                $sessionRisk = if ($riskFactors.MaxSessions -gt 100) { 0.2 } elseif ($riskFactors.MaxSessions -gt 75) { 0.1 } else { 0.05 }
                $errorRisk = if ($riskFactors.ErrorRate -gt 0.05) { 0.3 } elseif ($riskFactors.ErrorRate -gt 0.02) { 0.15 } else { 0.05 }
                
                $failureProbability = ($cpuRisk + $memoryRisk + $sessionRisk + $errorRisk) / 4
                
                $factors += [PSCustomObject]@{ Factor = "High CPU Usage"; Impact = $cpuRisk; Value = "$([math]::Round($riskFactors.AverageCPU, 2))%" }
                $factors += [PSCustomObject]@{ Factor = "High Memory Usage"; Impact = $memoryRisk; Value = "$([math]::Round($riskFactors.AverageMemory, 2))%" }
                $factors += [PSCustomObject]@{ Factor = "High Session Count"; Impact = $sessionRisk; Value = $riskFactors.MaxSessions }
                $factors += [PSCustomObject]@{ Factor = "Error Rate"; Impact = $errorRisk; Value = "$([math]::Round($riskFactors.ErrorRate * 100, 2))%" }
            }
            "Database" {
                # For databases, IO, locks, and query performance are key factors
                $ioRisk = if ($riskFactors.AverageIO -gt 80) { 0.4 } elseif ($riskFactors.AverageIO -gt 60) { 0.2 } else { 0.05 }
                $lockRisk = if ($riskFactors.LockWaitTime -gt 5000) { 0.3 } elseif ($riskFactors.LockWaitTime -gt 2000) { 0.15 } else { 0.05 }
                $queryRisk = if ($riskFactors.AverageQueryTime -gt 2000) { 0.3 } elseif ($riskFactors.AverageQueryTime -gt 500) { 0.15 } else { 0.05 }
                $cpuRisk = if ($riskFactors.AverageCPU -gt 85) { 0.2 } elseif ($riskFactors.AverageCPU -gt 75) { 0.1 } else { 0.05 }
                
                $failureProbability = ($ioRisk + $lockRisk + $queryRisk + $cpuRisk) / 4
                
                $factors += [PSCustomObject]@{ Factor = "High IO Usage"; Impact = $ioRisk; Value = "$([math]::Round($riskFactors.AverageIO, 2))%" }
                $factors += [PSCustomObject]@{ Factor = "Lock Wait Time"; Impact = $lockRisk; Value = "$([math]::Round($riskFactors.LockWaitTime, 2))ms" }
                $factors += [PSCustomObject]@{ Factor = "Query Response Time"; Impact = $queryRisk; Value = "$([math]::Round($riskFactors.AverageQueryTime, 2))ms" }
                $factors += [PSCustomObject]@{ Factor = "CPU Usage"; Impact = $cpuRisk; Value = "$([math]::Round($riskFactors.AverageCPU, 2))%" }
            }
            "BatchServer" {
                # For batch servers, pending jobs and execution times are key factors
                $backlogRisk = if ($riskFactors.BatchBacklog -gt 50) { 0.4 } elseif ($riskFactors.BatchBacklog -gt 20) { 0.25 } else { 0.05 }
                $failureRisk = if ($riskFactors.BatchFailureRate -gt 0.15) { 0.3 } elseif ($riskFactors.BatchFailureRate -gt 0.05) { 0.15 } else { 0.05 }
                $runtimeRisk = if ($riskFactors.AverageBatchTime -gt 120) { 0.2 } elseif ($riskFactors.AverageBatchTime -gt 60) { 0.1 } else { 0.05 }
                $cpuRisk = if ($riskFactors.AverageCPU -gt 85) { 0.1 } elseif ($riskFactors.AverageCPU -gt 75) { 0.05 } else { 0.02 }
                
                $failureProbability = ($backlogRisk + $failureRisk + $runtimeRisk + $cpuRisk) / 4
                
                $factors += [PSCustomObject]@{ Factor = "Batch Backlog"; Impact = $backlogRisk; Value = $riskFactors.BatchBacklog }
                $factors += [PSCustomObject]@{ Factor = "Batch Failure Rate"; Impact = $failureRisk; Value = "$([math]::Round($riskFactors.BatchFailureRate * 100, 2))%" }
                $factors += [PSCustomObject]@{ Factor = "Avg Batch Execution Time"; Impact = $runtimeRisk; Value = "$([math]::Round($riskFactors.AverageBatchTime, 2))min" }
                $factors += [PSCustomObject]@{ Factor = "CPU Usage"; Impact = $cpuRisk; Value = "$([math]::Round($riskFactors.AverageCPU, 2))%" }
            }
        }
        
        # Adjust for trending behavior (getting worse over time)
        $trendFactor = Calculate-TrendFactor -Data $ComponentData -Features $Features
        $failureProbability = [math]::Min(1.0, $failureProbability * (1 + $trendFactor))
        
        # Determine risk level
        $result.FailureProbability = [math]::Round($failureProbability, 3)
        $result.RiskLevel = if ($failureProbability -gt 0.7) { "Critical" } 
                           elseif ($failureProbability -gt 0.4) { "High" }
                           elseif ($failureProbability -gt 0.2) { "Medium" }
                           else { "Low" }
        
        $result.ContributingFactors = $factors
        
        # Generate recommended actions based on risk factors and component type
        $result.RecommendedActions = @(Get-RecommendedActions -ComponentType $ComponentType -RiskFactors $riskFactors -RiskLevel $result.RiskLevel)
        
        $result.Message = "Failure probability for $ComponentType is $($result.FailureProbability) ($($result.RiskLevel) risk)"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Component failure prediction failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Calculate-RiskIndicators {
    <#
    .SYNOPSIS
    Calculates risk indicators from component performance data
    
    .DESCRIPTION
    This function computes various metrics that indicate potential component risk.
    
    .PARAMETER Data
    Component performance data
    
    .PARAMETER Features
    Features to calculate indicators for
    
    .EXAMPLE
    $indicators = Calculate-RiskIndicators -Data $data -Features @("CPU_Usage", "Memory_Usage")
    
    .NOTES
    This function extracts key risk indicators from performance data.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Data,
        
        [Parameter(Mandatory=$true)]
        [string[]]$Features
    )
    
    $indicators = @{
        AverageCPU = 0
        AverageMemory = 0
        AverageIO = 0
        MaxSessions = 0
        ErrorRate = 0
        LockWaitTime = 0
        AverageQueryTime = 0
        BatchBacklog = 0
        BatchFailureRate = 0
        AverageBatchTime = 0
    }
    
    # Calculate averages for numeric features
    foreach ($feature in $Features) {
        $values = $Data | Where-Object { $_.$feature -ne $null } | ForEach-Object { $_.$feature }
        $numericValues = $values | Where-Object { $_ -is [int] -or $_ -is [double] -or $_ -is [float] -or $_ -is [decimal] -or $_ -is [long] }
        
        if ($numericValues.Count -gt 0) {
            $avgValue = ($numericValues | Measure-Object -Average).Average
            $maxValue = ($numericValues | Measure-Object -Maximum).Maximum
            
            # Map to appropriate indicator based on feature name
            switch ($feature) {
                "CPU_Usage" { $indicators.AverageCPU = $avgValue }
                "Memory_Usage" { $indicators.AverageMemory = $avgValue }
                "Disk_IO" { $indicators.AverageIO = $avgValue }
                "Active_Sessions" { $indicators.MaxSessions = $maxValue }
                "Error_Count" { $indicators.ErrorRate = $avgValue }
                "Lock_Wait_Time" { $indicators.LockWaitTime = $avgValue }
                "Query_Response_Time" { $indicators.AverageQueryTime = $avgValue }
                "Batch_Backlog" { $indicators.BatchBacklog = $avgValue }
                "Batch_Failure_Rate" { $indicators.BatchFailureRate = $avgValue }
                "Average_Batch_Time" { $indicators.AverageBatchTime = $avgValue }
            }
        }
    }
    
    return $indicators
}

function Calculate-TrendFactor {
    <#
    .SYNOPSIS
    Calculates a trend factor indicating if metrics are getting worse
    
    .DESCRIPTION
    This function determines if key metrics are trending in a concerning direction.
    
    .PARAMETER Data
    Component performance data
    
    .PARAMETER Features
    Features to analyze trends for
    
    .EXAMPLE
    $trend = Calculate-TrendFactor -Data $data -Features @("CPU_Usage", "Memory_Usage")
    
    .NOTES
    This function uses linear regression to identify concerning trends.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Data,
        
        [Parameter(Mandatory=$true)]
        [string[]]$Features
    )
    
    $trendFactor = 0
    
    # Sort data by timestamp
    $sortedData = $Data | Sort-Object { [datetime]$_.Timestamp }
    
    # For each feature, calculate trend
    foreach ($feature in $Features) {
        $values = @()
        for ($i = 0; $i -lt $sortedData.Count; $i++) {
            $value = $sortedData[$i].$feature
            if ($value -ne $null -and ($value -is [int] -or $value -is [double] -or $value -is [float] -or $value -is [decimal] -or $value -is [long])) {
                $values += @($i, $value)
            }
        }
        
        if ($values.Count -gt 5) {
            # Simple linear regression to calculate trend slope
            $n = $values.Count / 2
            $sumX = 0
            $sumY = 0
            $sumXY = 0
            $sumX2 = 0
            
            for ($i = 0; $i -lt $n; $i++) {
                $x = $values[$i * 2]
                $y = $values[($i * 2) + 1]
                
                $sumX += $x
                $sumY += $y
                $sumXY += $x * $y
                $sumX2 += $x * $x
            }
            
            $denominator = $n * $sumX2 - $sumX * $sumX
            if ($denominator -ne 0) {
                $slope = ($n * $sumXY - $sumX * $sumY) / $denominator
                
                # Normalize the slope relative to the average value
                $avgY = $sumY / $n
                if ($avgY -ne 0) {
                    $normalizedSlope = $slope / $avgY
                    
                    # Only consider upward trends (getting worse) for risk
                    if ($normalizedSlope -gt 0) {
                        $trendFactor += $normalizedSlope
                    }
                }
            }
        }
    }
    
    # Apply a cap to the trend factor to prevent extreme values
    $trendFactor = [math]::Min(1.0, [math]::Abs($trendFactor))
    
    return $trendFactor
}

function Get-RecommendedActions {
    <#
    .SYNOPSIS
    Gets recommended actions based on component type and risk factors
    
    .DESCRIPTION
    This function provides specific recommendations for addressing identified risks.
    
    .PARAMETER ComponentType
    Type of component at risk
    
    .PARAMETER RiskFactors
    Risk factors identified for the component
    
    .PARAMETER RiskLevel
    Overall risk level determined
    
    .EXAMPLE
    $actions = Get-RecommendedActions -ComponentType "AOS" -RiskFactors $factors -RiskLevel "High"
    
    .NOTES
    This function provides actionable recommendations for risk mitigation.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [string]$ComponentType,
        
        [Parameter(Mandatory=$true)]
        [hashtable]$RiskFactors,
        
        [Parameter(Mandatory=$true)]
        [string]$RiskLevel
    )
    
    $actions = @()
    
    # Add recommendations based on component type and risk factors
    switch ($ComponentType) {
        "AOS" {
            if ($RiskFactors.AverageCPU -gt 80) {
                $actions += "Scale up AOS server resources or add additional AOS instances"
            }
            if ($RiskFactors.AverageMemory -gt 85) {
                $actions += "Increase RAM allocation or optimize memory usage in AX configuration"
            }
            if ($RiskFactors.MaxSessions -gt 75) {
                $actions += "Review user session management and consider load balancing"
            }
            if ($RiskFactors.ErrorRate -gt 0.02) {
                $actions += "Review application logs and recent code changes for errors"
            }
        }
        "Database" {
            if ($RiskFactors.AverageIO -gt 70) {
                $actions += "Investigate disk performance and consider faster storage"
            }
            if ($RiskFactors.LockWaitTime -gt 2000) {
                $actions += "Analyze blocking queries and optimize indexes"
            }
            if ($RiskFactors.AverageQueryTime -gt 500) {
                $actions += "Review and optimize slow queries, update statistics"
            }
        }
        "BatchServer" {
            if ($RiskFactors.BatchBacklog -gt 20) {
                $actions += "Add batch server instances or optimize batch scheduling"
            }
            if ($RiskFactors.BatchFailureRate -gt 0.05) {
                $actions += "Investigate failed batch jobs and their root causes"
            }
            if ($RiskFactors.AverageBatchTime -gt 60) {
                $actions += "Review batch job complexity and optimize performance"
            }
        }
    }
    
    # Add general recommendations based on risk level
    if ($RiskLevel -eq "Critical" -or $RiskLevel -eq "High") {
        $actions += "Schedule maintenance window for preventive actions"
        $actions += "Increase monitoring frequency for this component"
    }
    
    if ($RiskLevel -eq "Critical") {
        $actions += "Consider failover to backup system if available"
        $actions += "Notify stakeholders about potential service impact"
    }
    
    return $actions
}

function Predict-MaintenanceSchedule {
    <#
    .SYNOPSIS
    Predicts optimal maintenance scheduling based on component usage patterns
    
    .DESCRIPTION
    This function analyzes usage patterns to recommend optimal times for maintenance
    that minimize business impact.
    
    .PARAMETER ComponentUsageData
    Historical usage data for the component
    
    .PARAMETER ComponentType
    Type of component
    
    .PARAMETER MaintenanceDuration
    Expected duration of maintenance (in hours)
    
    .EXAMPLE
    $schedule = Predict-MaintenanceSchedule -ComponentUsageData $data -ComponentType "AOS" -MaintenanceDuration 2
    
    .NOTES
    This function helps schedule maintenance during low-usage periods.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$ComponentUsageData,
        
        [Parameter(Mandatory=$true)]
        [ValidateSet("AOS", "Database", "BatchServer", "ReportServer", "FileServer", "Application")]
        [string]$ComponentType,
        
        [Parameter()]
        [int]$MaintenanceDuration = 2
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Maintenance schedule prediction completed successfully"
        RecommendedWindows = @()
        BestTime = $null
        ConfidenceScore = 0.85
    }
    
    try {
        if ($ComponentUsageData.Count -lt 10) {
            $result.Status = "Error"
            $result.Message = "Insufficient usage data for schedule prediction"
            $result.ConfidenceScore = 0.4
            return $result
        }
        
        # Extract time-based patterns
        $hourlyUsage = @{}
        $dailyUsage = @{}
        
        foreach ($record in $ComponentUsageData) {
            $timestamp = [datetime]$record.Timestamp
            $hour = $timestamp.Hour
            $dayOfWeek = $timestamp.DayOfWeek.Value__
            
            # Get the primary usage metric for this component type
            $usageMetric = switch ($ComponentType) {
                "AOS" { $record.Active_Sessions }
                "Database" { $record.Connection_Count }
                "BatchServer" { $record.Running_Batch_Jobs }
                "ReportServer" { $record.Active_Reports }
                "FileServer" { $record.Active_Connections }
                "Application" { $record.Request_Rate }
            }
            
            if ($usageMetric -ne $null) {
                # Add to hourly usage
                if (-not $hourlyUsage.ContainsKey($hour)) {
                    $hourlyUsage[$hour] = @()
                }
                $hourlyUsage[$hour] += $usageMetric
                
                # Add to daily usage
                if (-not $dailyUsage.ContainsKey($dayOfWeek)) {
                    $dailyUsage[$dayOfWeek] = @()
                }
                $dailyUsage[$dayOfWeek] += $usageMetric
            }
        }
        
        # Calculate average usage by hour and day
        $hourlyAvg = @{}
        $dailyAvg = @{}
        
        foreach ($hour in $hourlyUsage.Keys) {
            $hourlyAvg[$hour] = ($hourlyUsage[$hour] | Measure-Object -Average).Average
        }
        
        foreach ($day in $dailyUsage.Keys) {
            $dailyAvg[$day] = ($dailyUsage[$day] | Measure-Object -Average).Average
        }
        
        # Find the best maintenance windows (lowest usage periods)
        $windows = @()
        
        # Consider each hour as a potential start time for maintenance
        for ($hour = 0; $hour -le 23; $hour++) {
            $totalExpectedUsage = 0
            $validWindow = $true
            
            # Calculate expected usage over the maintenance duration
            for ($i = 0; $i -lt $MaintenanceDuration; $i++) {
                $checkHour = ($hour + $i) % 24
                if ($hourlyAvg.ContainsKey($checkHour)) {
                    $totalExpectedUsage += $hourlyAvg[$checkHour]
                } else {
                    $totalExpectedUsage += 0
                }
            }
            
            if ($validWindow) {
                $averageWindowUsage = $totalExpectedUsage / $MaintenanceDuration
                $windows += [PSCustomObject]@{
                    StartTime = $hour
                    EndTime = ($hour + $MaintenanceDuration) % 24
                    AverageUsage = $averageWindowUsage
                }
            }
        }
        
        # Sort by lowest expected usage
        $recommendedWindows = $windows | Sort-Object AverageUsage | Select-Object -First 5
        
        foreach ($window in $recommendedWindows) {
            # Convert hour to a more descriptive time format
            $startTimeDesc = $window.StartTime
            $endTimeDesc = $window.EndTime
            
            $result.RecommendedWindows += [PSCustomObject]@{
                TimeRange = "$startTimeDesc:00 - $endTimeDesc:00"
                StartHour = $window.StartTime
                EndHour = $window.EndTime
                ExpectedUsage = $window.AverageUsage
                Suitability = if ($window.AverageUsage -lt 5) { "Excellent" }
                             elseif ($window.AverageUsage -lt 15) { "Good" }
                             elseif ($window.AverageUsage -lt 30) { "Fair" }
                             else { "Poor" }
            }
        }
        
        if ($result.RecommendedWindows.Count -gt 0) {
            $result.BestTime = $result.RecommendedWindows[0]
        }
        
        $result.Message = "Identified $($result.RecommendedWindows.Count) low-usage maintenance windows"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Maintenance schedule prediction failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Assess-HardwareDegradation {
    <#
    .SYNOPSIS
    Assesses hardware degradation patterns that may lead to component failures
    
    .DESCRIPTION
    This function analyzes performance metrics over time to identify patterns
    that might indicate hardware degradation.
    
    .PARAMETER HistoricalMetrics
    Historical performance metrics over an extended period
    
    .PARAMETER ComponentID
    Identifier for the component being assessed
    
    .PARAMETER MetricTrends
    Specific metrics to analyze for degradation
    
    .EXAMPLE
    $degradation = Assess-HardwareDegradation -HistoricalMetrics $metrics -ComponentID "AOS-01"
    
    .NOTES
    This function identifies gradual hardware performance degradation over time.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$HistoricalMetrics,
        
        [Parameter(Mandatory=$true)]
        [string]$ComponentID,
        
        [Parameter()]
        [string[]]$MetricTrends = @("Response_Time", "CPU_Usage", "Memory_Usage")
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Hardware degradation assessment completed successfully"
        DegradationIndicators = @()
        TrendAnalysis = @{}
        HealthScore = 100
        ConfidenceScore = 0.80
    }
    
    try {
        if ($HistoricalMetrics.Count -lt 20) {
            $result.Status = "Warning"
            $result.Message = "Limited historical data for comprehensive degradation analysis"
            $result.ConfidenceScore = 0.5
            return $result
        }
        
        # Sort by timestamp
        $sortedMetrics = $HistoricalMetrics | Sort-Object { [datetime]$_.Timestamp }
        
        # Analyze degradation patterns for each metric
        $degradationIndicators = @()
        $trendAnalysis = @{}
        
        foreach ($metric in $MetricTrends) {
            # Extract values over time
            $timeValuePairs = @()
            for ($i = 0; $i -lt $sortedMetrics.Count; $i++) {
                $value = $sortedMetrics[$i].$metric
                if ($value -ne $null -and ($value -is [int] -or $value -is [double] -or $value -is [float] -or $value -is [decimal] -or $value -is [long])) {
                    $timeValuePairs += @($i, $value)
                }
            }
            
            if ($timeValuePairs.Count -gt 5) {
                # Calculate trend using linear regression
                $n = $timeValuePairs.Count / 2
                $sumX = 0
                $sumY = 0
                $sumXY = 0
                $sumX2 = 0
                
                for ($i = 0; $i -lt $n; $i++) {
                    $x = $timeValuePairs[$i * 2]  # Index as time proxy
                    $y = $timeValuePairs[($i * 2) + 1]  # Value
                    
                    $sumX += $x
                    $sumY += $y
                    $sumXY += $x * $y
                    $sumX2 += $x * $x
                }
                
                $denominator = $n * $sumX2 - $sumX * $sumX
                if ($denominator -ne 0) {
                    $slope = ($n * $sumXY - $sumX * $sumY) / $denominator
                    
                    # Calculate R-squared to measure trend strength
                    $meanY = $sumY / $n
                    $ssTotal = 0
                    $ssResidual = 0
                    
                    for ($i = 0; $i -lt $n; $i++) {
                        $x = $timeValuePairs[$i * 2]
                        $y = $timeValuePairs[($i * 2) + 1]
                        $predictedY = (($sumY * $sumX2 - $sumX * $sumXY) / $denominator) + $slope * $x
                        $ssTotal += [math]::Pow($y - $meanY, 2)
                        $ssResidual += [math]::Pow($y - $predictedY, 2)
                    }
                    
                    $rSquared = if ($ssTotal -ne 0) { 1 - ($ssResidual / $ssTotal) } else { 0 }
                    
                    $trendAnalysis[$metric] = @{
                        Slope = $slope
                        R_squared = $rSquared
                        IsDegrading = $slope -gt 0 -and $rSquared -gt 0.3  # Positive slope indicates degradation
                        TrendStrength = $rSquared
                        Direction = if ($slope -gt 0) { "Increasing" } elseif ($slope -lt 0) { "Decreasing" } else { "Stable" }
                    }
                    
                    # Add to degradation indicators if concerning
                    if ($trendAnalysis[$metric].IsDegrading) {
                        $degradationIndicators += [PSCustomObject]@{
                            Metric = $metric
                            Trend = $trendAnalysis[$metric].Direction
                            Rate = $trendAnalysis[$metric].Slope
                            Confidence = $trendAnalysis[$metric].TrendStrength
                            Description = "$metric is degrading with R²=$([math]::Round($trendAnalysis[$metric].TrendStrength, 3))"
                        }
                    }
                }
            }
        }
        
        $result.DegradationIndicators = $degradationIndicators
        $result.TrendAnalysis = $trendAnalysis
        
        # Calculate overall health score
        $healthImpact = 0
        foreach ($indicator in $degradationIndicators) {
            # Higher confidence and rate of degradation reduce health score more
            $impact = $indicator.Confidence * 30  # Max 30 points off for each metric
            $healthImpact += $impact
        }
        
        $result.HealthScore = [math]::Max(0, [math]::Round(100 - $healthImpact))
        
        # Adjust confidence based on amount of data and trend clarity
        $result.ConfidenceScore = [math]::Min(0.95, [math]::Max(0.6, 0.4 + ($sortedMetrics.Count / 200)))
        
        $result.Message = "Identified $($degradationIndicators.Count) degradation indicators for component $ComponentID. Health Score: $($result.HealthScore)/100"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Hardware degradation assessment failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

function Generate-PredictiveMaintenanceReport {
    <#
    .SYNOPSIS
    Generates a comprehensive predictive maintenance report
    
    .DESCRIPTION
    This function combines all predictive maintenance analyses into a comprehensive report.
    
    .PARAMETER Components
    List of components to assess
    
    .PARAMETER ReportPeriod
    Period for the report (e.g., "Week", "Month", "Quarter")
    
    .EXAMPLE
    $report = Generate-PredictiveMaintenanceReport -Components $componentList -ReportPeriod "Week"
    
    .NOTES
    This function provides an executive summary of predictive maintenance insights.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [object[]]$Components,
        
        [Parameter()]
        [ValidateSet("Day", "Week", "Month", "Quarter")]
        [string]$ReportPeriod = "Week"
    )
    
    # Initialize result object
    $result = @{
        Status = "Success"
        Message = "Predictive maintenance report generated successfully"
        Report = @{}
        PriorityComponents = @()
        RecommendedActions = @()
        ConfidenceScore = 0.90
    }
    
    try {
        $allAssessments = @()
        $allPredictions = @()
        $priorityComponents = @()
        
        foreach ($component in $Components) {
            # Perform failure prediction
            $failurePred = Predict-ComponentFailure -ComponentData $component.HistoricalData -ComponentType $component.Type -LookaheadPeriod 72
            
            # Perform hardware degradation assessment
            $degradationAssessment = Assess-HardwareDegradation -HistoricalMetrics $component.HistoricalData -ComponentID $component.Id
            
            # Perform maintenance scheduling
            $schedulePred = if ($failurePred.RiskLevel -ne "Low") {
                Predict-MaintenanceSchedule -ComponentUsageData $component.UsageData -ComponentType $component.Type
            } else { $null }
            
            $assessment = [PSCustomObject]@{
                ComponentId = $component.Id
                ComponentType = $component.Type
                Location = $component.Location
                FailureProbability = $failurePred.FailureProbability
                RiskLevel = $failurePred.RiskLevel
                HealthScore = $degradationAssessment.HealthScore
                DegradationIndicators = $degradationAssessment.DegradationIndicators
                RecommendedActions = $failurePred.RecommendedActions
                MaintenanceWindow = $schedulePred.BestTime
                LastUpdated = Get-Date
            }
            
            $allAssessments += $assessment
            
            # Add to priority list if high risk
            if ($failurePred.RiskLevel -eq "Critical" -or $failurePred.RiskLevel -eq "High" -or $degradationAssessment.HealthScore -lt 70) {
                $priorityComponents += $assessment
            }
        }
        
        # Sort priority components by risk
        $priorityComponents = $priorityComponents | Sort-Object { 
            $riskOrder = @{"Critical" = 4; "High" = 3; "Medium" = 2; "Low" = 1}
            $riskOrder[$_.RiskLevel] 
        } -Descending, HealthScore
        
        # Generate summary statistics
        $summary = [PSCustomObject]@{
            TotalComponentsAssessed = $allAssessments.Count
            CriticalRiskComponents = ($allAssessments | Where-Object { $_.RiskLevel -eq "Critical" }).Count
            HighRiskComponents = ($allAssessments | Where-Object { $_.RiskLevel -eq "High" }).Count
            MediumRiskComponents = ($allAssessments | Where-Object { $_.RiskLevel -eq "Medium" }).Count
            AverageHealthScore = [math]::Round(($allAssessments | Measure-Object -Property HealthScore -Average).Average, 2)
            ReportPeriod = $ReportPeriod
            GeneratedAt = Get-Date
            PriorityComponentsCount = $priorityComponents.Count
        }
        
        # Aggregate recommended actions
        $allActions = @()
        foreach ($assessment in $allAssessments) {
            foreach ($action in $assessment.RecommendedActions) {
                $allActions += [PSCustomObject]@{
                    Component = $assessment.ComponentId
                    RiskLevel = $assessment.RiskLevel
                    Action = $action
                    Category = switch ($action.Substring(0, [math]::Min(10, $action.Length))) {
                        { $_ -like "*Scale*" -or $_ -like "*increase*" } { "Capacity" }
                        { $_ -like "*Review*" -or $_ -like "*analyze*" } { "Analysis" }
                        { $_ -like "*Schedule*" -or $_ -like "*maintenance*" } { "Scheduling" }
                        { $_ -like "*Optimize*" -or $_ -like "*update*" } { "Optimization" }
                        default { "General" }
                    }
                }
            }
        }
        
        $result.Report = @{
            Summary = $summary
            AllAssessments = $allAssessments
            PriorityComponents = $priorityComponents
            RecommendedActions = $allActions
        }
        
        $result.PriorityComponents = $priorityComponents
        $result.RecommendedActions = $allActions
        
        $result.Message = "Generated predictive maintenance report for $($summary.TotalComponentsAssessed) components with $($summary.PriorityComponentsCount) requiring immediate attention"
    }
    catch {
        $result.Status = "Error"
        $result.Message = "Predictive maintenance report generation failed: $($_.Exception.Message)"
        $result.ConfidenceScore = 0.0
        Write-Error $result.Message
    }
    
    return $result
}

# Export functions
Export-ModuleMember -Function Predict-ComponentFailure, Predict-MaintenanceSchedule, Assess-HardwareDegradation, Generate-PredictiveMaintenanceReport